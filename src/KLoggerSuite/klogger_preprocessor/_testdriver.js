﻿"use strict";

process.env["AWS_REGION"] = "ap-northeast-2";
process.env["is_local"] = 1;
process.env["slack_webhook_url"] = "https://hooks.slack.com/services/...";
process.env["slack_webhook_channel"] = "z_klogger_test";

const fs = require("fs");
const app = require("./app");
const event = JSON.parse(fs.readFileSync("./sample.json", "utf8").trim());

function sleep(ms) {
    return new Promise((resolve, reject) => setTimeout(resolve, ms));
}

const run = async () => {
    const res = await app.handler(event);
    // console.log(res);

    while (true) {
        console.log("fin.");
        await sleep(5000);
    }
};

if (process.env.is_local !== 1) {
    process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

run();