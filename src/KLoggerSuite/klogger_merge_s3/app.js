"use strict";

const AWS = require("aws-sdk");
AWS.config.update({ region: process.env.aws_region });
if (process.env.is_local != null) {
    AWS.config.credentials = new AWS.SharedIniFileCredentials({ profile: 'klogger' });
    process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

const s3 = new AWS.S3();
const crypto = require("crypto");
const request = require("request-promise");

const MB_5 = 5242880;            // S3상에서 머지(멀티파트업로드)가 가능한 최소 크기(5MB).
const MB_LARGE = MB_5 * 20;      // 최종 머지할 크기(100M 이상~).
const SUFFIX_MERGED = "_fin";    // 최종 머지 파일 이름의 마지막에 붙을 문자열.

var context = null;

exports.handler = async (event) => {
    var prefix;
    if (event && event.prefix) {
        prefix = event.prefix;
    } else {
        // 시간이 넘어가도 이전 시간에 대해 최종 머지할 수 있도록 시간을 뒤로 당긴다(호출 주기보다 커야 한다).
        const addMinute = event && event.intervalMin ? -(event.intervalMin + 5) : -10;
        prefix = buildPrefixByDate(addMinute);
    }
    
    console.log(`prefix: ${prefix}`);

    context = {
        prefix: prefix,
        bucket: process.env.bucket_name
    };

    try {
        await merge();
    } catch (exception) {
        console.error("=================== exception beg ===================");
        console.error(exception);
        await sendSlack("EXCEPTION", `\`\`\`${exception.stack}\`\`\``);
        console.error("=================== exception end ===================");
    }

    return "fin.";
};

function buildPrefixByDate(addMinute = 0) {
    const date = new Date(new Date().toUTCString());    // firehose가 UTC기반으로 시간까지 prefix를 붙여주는 것을 활용한다.
    date.setMinutes(date.getMinutes() + addMinute);
    const isoDate = date.toISOString();                 // 2019-08-12T13:25:08.000Z
    const tokens = isoDate.split("T");                  // [2019-08-12, 13:25:08.000Z]
    const ymd = tokens[0].replace(/\-/g, "/");          // 2019/08/12
    const hour = tokens.pop().split(":")[0];            // 13

    return `${ymd}/${hour}`; // "2019/08/05/13"
}

async function merge() {
    // 온라인(S3)에서 머지할 수 없는 작은(5MB 이하)파일에 대한 머지.
    var success = await mergeSmallFile();
    if (success === false) {
        console.error("error mergeSmallFile");
        return;
    }

    console.log("ok. small file merge.");

    // 멀티파트업로드를 이용해 온라인(S3)에서 머지할 수 있는 파일에 대한 머지.
    success = await mergeLargeFile();
    if (success === false) {
        console.error("error mergeLargeFile");
        return;
    }

    console.log("ok. large file merge.");
}

async function mergeSmallFile() {
    // 전체 리스트 읽음.
    const s3Objects = await s3.listObjects({
        Bucket: context.bucket,
        Prefix: context.prefix
    }).promise();

    // S3 상에서 머지가 불가능한 5MB 미만 파일 선택.
    const under5MBs = selectObjectMinMax(s3Objects, 0, MB_5);

    // 작은 것부터 머지할 수 있도록 오름차순 정렬.
    under5MBs.sort((l, r) => l.Size - r.Size);

    // 5MB 이상이 되도록 묶음.
    const smallBundles = makeOverNSizeBundles(under5MBs, MB_5);

    // 묶은 번들들을 로컬(람다)에서 머지하고 업로드.
    const merges = await mergeOnLambda(smallBundles);

    // 머지한 파일들 삭제.
    const count = await deleteS3Objects(merges);

    return count === merges.length;
}

async function mergeLargeFile() {
    // 전체 리스트를 읽음.
    const s3Objects = await s3.listObjects({
        Bucket: context.bucket,
        Prefix: context.prefix
    }).promise();

    // S3 상에서 머지가 가능한 5MB 이상 파일 선택.
    const over5MBs = selectObjectMinMax(s3Objects, MB_5, MB_LARGE);
    if (over5MBs.length <= 1) {
        console.log("There are no files to merge.");    // 병합할 파일이 없음.
        return true;
    }

    // 작은 것부터 머지할 수 있도록 오름차순 정렬.
    over5MBs.sort((l, r) => l.Size - r.Size);

    // MB_LARGE 크기 이상이 되도록 묶음.
    const largeBundles = makeOverNSizeBundles(over5MBs, MB_LARGE);

    // 다 합해도 MB_LARGE 보다 크지 않으므로 남은 것들을 하나로 머지.
    if (largeBundles.length <= 0) {
        largeBundles.push(over5MBs);
    }

    const merges = await mergeOnS3(largeBundles);

    const count = await deleteS3Objects(merges);

    return count === merges.length;
}

// min 보다 크고(초과), max 보다 작은(이하) S3Object 배열 반환.
function selectObjectMinMax(s3Objects, min, max, skipSuffix = null) {
    const selectObjects = [];
    s3Objects.Contents.forEach(content => {
        if (skipSuffix && content.Key.endsWith(skipSuffix)) {
            console.log(`skip: ${content.Key}`);
            return;
        }

        if (content.Size > min && content.Size <= max) {
            selectObjects.push(content);
        }
    });

    return selectObjects;
}

// size 보다 큰 번들의 리스트로 묶는다. [[번들], [번들], [번들], ...]
function makeOverNSizeBundles(s3Objects, size, skipSuffix = null) {
    const overNSizeBundles = [];

    var bundleSize = 0;
    var bundle = [];
    s3Objects.forEach(content => {
        if (skipSuffix && content.Key.endsWith(skipSuffix)) {
            console.log(`skip: ${content.Key}`);
            return;
        }

        bundle.push(content);
        bundleSize += content.Size;

        if (bundleSize > size) {
            overNSizeBundles.push(bundle);
            bundle = [];
            bundleSize = 0;
        }
    });

    // 마지막 번들이 size를 넘지 못해 남은 경우.
    if (bundle.length > 0) {
        // 이미 묶인 마지막 번들에 추가한다.
        if (overNSizeBundles.length > 0) {
            overNSizeBundles[overNSizeBundles.length - 1].push(...bundle);
        } else {
            // 조건을 넘는 번들이 하나도 없다. 즉, 입력을 다 더해도 size가 안된다.
        }
    }

    return overNSizeBundles;
}

async function mergeOnLambda(bundles) {
    const tasks = [];
    for (const bundle of bundles) {
        const task = mergeOnLambdaBundle(bundle);
        tasks.push(task);
    }

    const merges = await Promise.all(tasks);

    return [].concat(...merges);
}

// bundle안에 있는 파일들을 받아서 하나의 파일로 합친 뒤 업로드한다.
async function mergeOnLambdaBundle(bundle) {
    var mergedBody = null;
    const fileName = buildMergedFileName(bundle, 1);

    for (const content of bundle) {
        const data = await s3.getObject({
            Bucket: context.bucket,
            Key: content.Key
        }).promise();

        mergedBody ? (mergedBody += data.Body) : (mergedBody = data.Body);

        console.log(`merge on lambda: ${content.Key}`);
    }

    const putResult = await s3.putObject({
        Bucket: context.bucket,
        Key: `${context.prefix}/${fileName}`,
        Body: mergedBody
    }).promise();

    if (putResult.ETag) {
        console.log(`complete merge on lambda: ${fileName}`);
    } else {
        console.error(`fail merge on lambda: ${fileName}`);
    }

    return bundle;
}

function buildMergedFileName(bundle, phase, suffix = null) {
    const sha1 = crypto.createHash("sha1");
    const nameJoined = bundle.map(content => content.Key).join();
    const hash = sha1.update(nameJoined, 'utf-8').digest('hex');
    var fileName = `merged_phase${phase}_${hash}_${bundle.length}`;
    if (suffix) {
        fileName += suffix;
    }

    return fileName;
}

async function deleteS3Objects(s3Objects) {
    if (s3Objects.length <= 0) {
        return 0;
    }

    const params = {
        Bucket: context.bucket,
        Delete: {
            Objects: []
        }
    };

    for (const content of s3Objects) {
        params.Delete.Objects.push({
            Key: content.Key
        });

        console.log(`delete: ${content.Key}`);
    }

    const result = await s3.deleteObjects(params).promise();

    return result.Deleted.length;
}

async function mergeOnS3(largeBundles) {
    const tasks = [];
    for (const bundle of largeBundles) {
        const task = mergeOnS3Bundle(bundle);
        tasks.push(task);
    }

    const merges = await Promise.all(tasks);

    return [].concat(...merges);
}

// bundle안에 있는 파일들을 멀티파트업로드를 이용하여 온라인(S3) 상에서 머지한다.
// https://docs.aws.amazon.com/AWSJavaScriptSDK/latest/AWS/S3.html#createMultipartUpload-property
async function mergeOnS3Bundle(bundle) {
    const fileName = buildMergedFileName(bundle, 2, SUFFIX_MERGED);

    const createResult = await s3.createMultipartUpload({
        Bucket: context.bucket,
        Key: `${context.prefix}/${fileName}`
    }).promise();

    var partNumber = 1;
    const partNumberAndTasks = [];
    for (const content of bundle) {
        const uploadPartCopyTask = s3.uploadPartCopy({
            Bucket: context.bucket,
            CopySource: `${context.bucket}/${content.Key}`, // 버킷 이름이 필요함에 유의.
            Key: createResult.Key,
            PartNumber: partNumber,
            UploadId: createResult.UploadId
        }).promise();

        partNumberAndTasks.push({ PartNumber: partNumber++, Task: uploadPartCopyTask });

        console.log(`merge on s3: ${content.Key}`);
    }

    const parts = [];
    for (const partNumberAndTask of partNumberAndTasks) {
        const uploadResult = await partNumberAndTask.Task;
        parts.push({ PartNumber: partNumberAndTask.PartNumber, ETag: uploadResult.CopyPartResult.ETag });
    }

    const completeResult = await s3.completeMultipartUpload({
        Bucket: context.bucket,
        Key: createResult.Key,
        MultipartUpload: {
            Parts: parts
        },
        UploadId: createResult.UploadId
    }).promise();

    if (completeResult.ETag) {
        console.log(`complete merge on s3: ${fileName}`);
    } else {
        console.error(`fail merge on s3: ${fileName}`);
    }

    return bundle;
}

async function sendSlack(title, text, color = "danger") {
    function formattedNow() {
        const date = new Date();
        date.setHours(date.getHours() + 9);
        const now = (date).toISOString().replace(/T/, " ").replace(/\..+/, "");
        return now;
    }

    if (process.env.slack_webhook_url == null) {
        console.error("there is no slack webhook url.");
        return;
    }

    const options = {
        method: "POST",
        url: process.env.slack_webhook_url,
        headers: {
            'Content-Type': "application/json"
        },
        json: {
            channel: process.env.slack_webhook_channel,
            username: "KLogger Merge Lambda",
            icon_emoji: ":ddo_bug_ne:",
            attachments: [
                {
                    "title": title,
                    "text": text,
                    "footer": `\`${formattedNow()}\``,
                    "mrkdwn": true
                }
            ]
        }
    };

    const res = await request.post(options);
    if (res !== "ok") {
        console.error("Fail Send Slack. Result: ", res);
    }
}