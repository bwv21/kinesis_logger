"use strict";

const AWS = require("aws-sdk");
AWS.config.update({ region: process.env.aws_region });

const s3 = new AWS.S3();
const athena = new AWS.Athena();

exports.handler = async (event) => {
    //console.log(JSON.stringify(event)); // 디버그용.

    try {
        var output = work(event);

        return { records: output };
    } catch (exception) {
        console.log(event);
        console.error("exception: ", exception);

        return { records: null };
    }
};

async function work(event) {
    const s3Objects = await s3.listObjects({
        Bucket: "klogger-dev.s3.dh-devcat.studio",
        Prefix: "/"
    }).promise();

    var params = {
        QueryString: "SELECT * FROM \"klogger_dev\".\"test_example_20190829_07\" limit 10;",
        ResultConfiguration: {
            EncryptionConfiguration: {
                EncryptionOption: 'SSE_S3'
            },
            OutputLocation: 's3://aws-athena-query-results-354053212618-ap-northeast-2'
        }
    };

    const res = await athena.startQueryExecution(params).promise();

    console.log(s3Objects);
}