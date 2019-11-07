"use strict";

const zlib = require("zlib");
const request = require("request-promise");

/*
 * 극단적으로 압축된 데이터를 풀 때 문제가 있을 수 있다(거의 발생하지 않겠지만 적어 놓는다).
 * 로직은 성공하지만 람다의 리턴이 6MB?로 제한되어 있어서 "body size is too long" 에러가 발생하며 전처리에 실패한다.
 */
exports.handler = async (event) => {
    //console.log(JSON.stringify(event)); // 디버그용.

    try {
        const output = await Promise.all(event.records.map(async (record) => ({
            recordId: record.recordId,
            result: "Ok",
            data: await preprocess(record.data)
        })));

        console.log("preprocessed: ", event.records.length);

        return { records: output };
    } catch (exception) {
        console.log(event);
        console.error("exception: ", exception);

        await sendSlack("EXCEPTION", `\`\`\`${exception.stack}\`\`\``);

        return { records: null };
    }
};

async function preprocess(putDataRaw) {
    const putDataString = new Buffer(putDataRaw, "base64").toString();
    const putData = JSON.parse(putDataString);  // putData는 C#의 PutData에 대응.
    
    if (putData.CompressedLog == null) {
        return putDataRaw;  // 압축한 로그가 아니다.
    }

    const unzipString = (await unzip(putData.CompressedLog)).toString();
    putData.Log = JSON.parse(unzipString);

    delete putData.CompressedLog;

    const preprocessedString = JSON.stringify(putData) + "\n";  // 개행으로 row를 구분한다(로그 시스템 규칙).
    const preprocessedB64 = Buffer.from(preprocessedString).toString("base64");

    return preprocessedB64;
}

function unzip(log) {
    return new Promise((resolve, reject) => {
        zlib.gunzip(new Buffer(log, "base64"), function (err, res) {
            if (err) {
                console.error("unzip error: ", err);
                reject(err);
            } else {
                resolve(res);
            }
        });
    });
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
            username: "KLogger Preprocessor Lambda",
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