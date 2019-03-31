////////////////////////////////////////////////////////////////////////
// Fetch spreadsheets.
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


var fs = require('fs');
var path = require('path');
var fetch = require('node-fetch');
var csvParseSync = require('csv-parse/lib/sync');


////////////////////////////////////////////////////////////////////////
// Globals.


var streamingAssetsOutputDirectory = '../../UnityJS_Optionality/Libraries/UnityJS_Optionality/StreamingAssets/UnityJS_Optionality';
var sheetOutputDirectory = streamingAssetsOutputDirectory + '/sheets';

var sheetsIndex = [];
var spreadsheetPromises = [];
var sheetPromises = [];

var spreadsheets = 
    csvParseSync(
        fs.readFileSync(
            'spreadsheets.tsv'),
        {
            delimiter: '\t',
            columns: true
        });


////////////////////////////////////////////////////////////////////////
// Delete the old spreadsheets from the sheet output directory.


fs.readdir(sheetOutputDirectory, function (err, fileNames) {
    if (err) {
        throw err;
    }
    fileNames.forEach(function (file) {
        var filePath = path.join(sheetOutputDirectory, file);
        console.log("deleting", filePath);
        fs.unlink(
            filePath,
            function (err) {
                if (err) {
                    throw err;
                }
            });
    });
});


////////////////////////////////////////////////////////////////////////
// Download the spreadsheets to the sheet output directory.


spreadsheets.forEach(function (spreadsheet, spreadsheetIndex) {
    console.log("spreadsheet:", spreadsheet);

    var spreadsheetName = spreadsheet.spreadsheetName;
    var spreadsheetID = spreadsheet.spreadsheetID;
    var spreadsheetServiceURL = spreadsheet.spreadsheetServiceURL;
    var url = spreadsheetServiceURL + '?sheets=1&metaData=1';

    spreadsheetPromises.push(
        fetch(url)
            .then(function (result) { return result.json(); })
            .then(function (result) {

                if (result.status != 'success') {
                    console.log("error fetching spreadsheet", spreadsheet);
                    return;
                }
                result.data.sheetNames.forEach(function (sheetName, sheetIndex) {
                    var sheet = result.data.sheets[sheetName];
                    sheetsIndex.push({
                        spreadsheetName: spreadsheetName,
                        spreadsheetIndex: spreadsheetIndex,
                        spreadsheetID: spreadsheetID,
                        sheetName: sheetName,
                        sheetIndex: sheetIndex,
                        sheetID: sheet.sheetID,
                        sheetFileName:
                            sheetName + '_' + spreadsheetID + '_' + sheet.sheetID + '.tsv',
                        sheetURL:
                            'https://docs.google.com/spreadsheets/d/' +
                            spreadsheetID + 
                            '/export?format=tsv&id=' +
                            spreadsheetID +
                            '&gid=' +
                            sheet.sheetID
                    });

                });

            }));

    });


// Write each sheet to the sheet output directory, wait for all sheets
// to download, then write out the sheets index.


Promise.all(spreadsheetPromises)
    .then(function () {

        sheetsIndex.forEach(function (sheet) {
            //console.log("Fetching spreadsheetName", spreadsheetName, "sheetName", sheetName);

            sheetPromises.push(
                fetch(sheet.sheetURL)
                    .then(function (result) { return result.text(); })
                    .then(function (result) {

                        var outputFileName = 
                            sheetOutputDirectory + '/' + sheet.sheetFileName;
                        //console.log("received sheetName", sheet.sheetName, "result.length", result.length, "outputFileName", outputFileName);

                        fs.writeFile(
                            outputFileName,
                            result,
                            function (err) {
                                if (err) {
                                    console.log("Error writing outputFileName:", outputFileName, "err:", err);
                                }
                            });

                    }));

            });

        Promise.all(sheetPromises)
            .then(function() {

                console.log("sheetsIndex", sheetsIndex);

                var jsCode = 
                    '// Index of sheets automatically generated by getsheets.js\n' +
                    'var gSheetsIndex = ' +
                    JSON.stringify(sheetsIndex) +
                    ';\n';

                //console.log(jsCode);

                var sheetsIndexFileName = 
                    streamingAssetsOutputDirectory + '/sheets-index.jss';

                fs.writeFile(
                    sheetsIndexFileName,
                    jsCode,
                    function (err) {
                        if (err) {
                            console.log("Error writing indexFileName:", sheetsIndexFileName, "err:", err);
                        }
                    });

            });

    });


////////////////////////////////////////////////////////////////////////