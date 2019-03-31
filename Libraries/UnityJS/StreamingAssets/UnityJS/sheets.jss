////////////////////////////////////////////////////////////////////////
// sheets.js
// By Don Hopkins, Ground Up Software.


////////////////////////////////////////////////////////////////////////


var gGoogleSheets = false;
try {
    if (typeof SpreadsheetApp == 'object') {
        gGoogleSheets = true;
    }
} catch (e) {}


////////////////////////////////////////////////////////////////////////


if (gGoogleSheets) {


   ////////////////////////////////////////////////////////////////////////
   // Google Sheets
   

   ////////////////////////////////////////////////////////////////////////


    console = {
        log: () => {
            var args = Array.prototype.slice.call(arguments);
            var str = args.join(' ');
            Logger.log(str);
        }
    };


    ////////////////////////////////////////////////////////////////////////
    // Set up the Goole Sheet menus when the spreadsheet is opened.


    function onOpen()
    {
        var ss = SpreadsheetApp.getActiveSpreadsheet();

        ss.addMenu("JSONster", [
            {name: 'Format Spreadsheet as JSON', functionName: 'FormatSpreadsheetAsJSON'},
            {name: 'Make Meta Data Sheets', functionName: 'MakeMetaDataSheets'},
            //{name: 'Update Meta Data', functionName: 'UpdateMetaData'},
            //{name: 'Make Named Ranges', functionName: 'MakeNamedRanges'},
            {name: 'Update Ranges', functionName: 'UpdateRanges'}
        ]);
    }


    ////////////////////////////////////////////////////////////////////////
    // Serve http requests.


    function doGet(e)
    {
        var startTime = new Date();
        var path = e ? e.pathInfo : '';
        var parameter = e ? e.parameter : {};
        var data = GetData(path, parameter);
        var endTime = new Date();
        var requestTime = endTime - startTime;

        var result = {
            status: 'success',
            requestTime: requestTime,
            data: data
        };

        var output = JSON.stringify(result);

        return ContentService.createTextOutput(output)
            .setMimeType(ContentService.MimeType.JSON);
    }


    ////////////////////////////////////////////////////////////////////////
    // Return a JSON representation of the sheets and named ranges.


    function GetData(path, parameter)
    {
        var ss = SpreadsheetApp.getActiveSpreadsheet();
        var spreadsheetName = ss.getName();
        var spreadsheetID = ss.getId();
        var data = {
            path: path,
            parameter: parameter,
            name: spreadsheetName,
            spreadsheetID: spreadsheetID
        };

        if (parameter.metaData) {
            //UpdateMetaData();
        }

        if (parameter.sheets) {

            data.sheets = {};
            data.sheetNames = [];

            var sheets = ss.getSheets();

            sheets.forEach((sheet, index) => {
                var sheetID = sheet.getSheetId();
                var sheetName = sheet.getName();
                var sheetRows = sheet.getLastRow();
                var sheetColumns = sheet.getLastColumn();

                data.sheetNames.push(sheetName);

                var sheetData = data.sheets[sheetName] = {
                    index: index,
                    sheetName: sheetName,
                    sheetID: sheetID,
                    spreadsheetID: spreadsheetID,
                    rows: sheetRows,
                    columns: sheetColumns
                };

                if (parameter.sheetValues) {
                    sheetData.values = sheet.getSheetValues(1, 1, sheetRows, sheetColumns);
                }

            });

        }

        if (parameter.ranges) {

            data.ranges = {};
            data.rangeNames = [];

            var namedRanges = ss.getNamedRanges();

            namedRanges.forEach((namedRange, index) => {
                var rangeName = namedRange.getName();
                var range = namedRange.getRange();
                var row = range.getRow();
                var column = range.getColumn();
                var rows = range.getHeight();
                var columns = range.getWidth();
                var sheet = range.getSheet();
                var sheetName = sheet.getName();
                var sheetID = sheet.getSheetId();

                data.rangeNames.push(rangeName);

                var rangeData = data.ranges[rangeName] = {
                    index: index,
                    rangeName: rangeName,
                    spreadsheetID: spreadsheetID,
                    sheetID: sheetID,
                    sheetName: sheetName,
                    row: row,
                    column: column,
                    rows: rows,
                    columns: columns
                };

                if (parameter.rangeValues) {
                    rangeData.values = 
                        sheet.getSheetValues(row, column, rows, columns);
                }

            });

        }

        return data;
    }


    function GetNamedRanges()
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var namedRanges = spreadsheet.getNamedRanges();
        var ranges = {};

        namedRanges.forEach((namedRange, index) => {
            var rangeName = namedRange.getName();
            var range = namedRange.getRange();
            var sheet = range.getSheet();

            var range = {
                rangeName: rangeName,
                sheetName: sheet.getName(),
                sheetID: sheet.getSheetId(),
                index: index,
                row: range.getRow(),
                column: range.getColumn(),
                rows: range.getNumRows(),
                columns: range.getNumColumns()
            };

            ranges[rangeName] = range;
        });

        return ranges;
    }


    function RemoveNamedRanges()
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var namedRanges = spreadsheet.getNamedRanges();

        namedRanges.forEach((namedRange) => {
            namedRange.remove();
        });
    }


    ////////////////////////////////////////////////////////////////////////
    // Parse the spreadsheet as JSON and render the formatting and outline.


    function FormatSpreadsheetAsJSON(sheetName, isSingleSheet)
    {
        if (!sheetName) {
            var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
            var sheet = spreadsheet.getActiveSheet();
            sheetName = sheet.getSheetName();
            isSingleSheet = true;
        }

        var sheets = {};
        var ranges = GetNamedRanges();

        var scope = 
            SheetToScope(sheets, ranges, sheetName, isSingleSheet);

        if (!scope) {
            console.log("sheets.js: SpreadsheetToJSON: SheetToScope returned null.");
        } else if (scope.error) {
            console.log("sheets.js: SpreadsheetToJSON: SheetToScope returned error: " + scope.error);
            return false;
        }

        //console.log("sheets.js: SpreadsheetToJSON: result: " + ((scope == null) ? "NULL" : JSON.stringify(scope.value)));

        FormatScope(sheets, ranges, scope);

        return true;
    }


    function FormatScope(sheets, ranges, scope)
    {
        var sheet =
            SpreadsheetApp.getActiveSpreadsheet().getSheetByName(scope.sheetName);
        var fullRange = 
            sheet.getRange(1, 1, sheet.getMaxRows(), sheet.getMaxColumns());

        ClearGroups(sheet, fullRange);
        ClearFormatting(sheet, fullRange);

        RenderScope(sheet, scope);
    }


    function ClearGroups(sheet, fullRange)
    {
        if (!sheet) {
            sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
        }

        if (!fullRange) {
            fullRange = sheet.getRange(1, 1, sheet.getMaxRows(), sheet.getMaxColumns());
        }

        fullRange.shiftRowGroupDepth(-1000);
        fullRange.shiftColumnGroupDepth(-1000);
        sheet.setRowGroupControlPosition(SpreadsheetApp.GroupControlTogglePosition.BEFORE);
    }


    function ClearFormatting(sheet, fullRange)
    {
        if (!sheet) {
            sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
        }

        if (!fullRange) {
            fullRange = sheet.getRange(1, 1, sheet.getMaxRows(), sheet.getMaxColumns());
        }

        fullRange.clearFormat();
    }


    function RenderScope(sheet, scope)
    {
        var dataRange = sheet.getDataRange();

        if (scope.inObject) {
            RenderObjectKey(sheet, dataRange, scope.row, scope.column, scope.rowCount, scope.columnCount, scope.keyRow, scope.keyColumn, scope.key, scope.index);
        }

        if (scope.inArray) {
            RenderArrayIndex(sheet, dataRange, scope.row, scope.column, scope.rowCount, scope.columnCount, scope.index);
        }

        if (scope.gotType) {
            RenderType(sheet, dataRange, scope.typeRow, scope.typeColumn, scope.typeName);
        }

        if (scope.gotValue) {
            RenderValue(sheet, dataRange, scope.valueRow, scope.valueColumn, scope.valueRows, scope.valueColumns);
        }

        if (scope.gotParams) {
            RenderParams(sheet, dataRange, scope.paramsRow, scope.paramsColumn, scope.paramsRows, scope.paramsColumns);
        }

        if (scope.hasChildRows) {
            //console.log("hasChildRows firstChildRow " + scope.firstChildRow + " lastChildRow " + scope.lastChildRow + " rowsUsed " + scope.rowsUsed);
            RenderChildRows(sheet, dataRange, scope.firstChildRow, scope.lastChildRow);
        }

        var comments = scope.comments;
        if (comments) {
            for (var commentIndex = 0, commentCount = comments.length;
                 commentIndex < commentCount;
                 commentIndex++) {
                var commentInfo = comments[commentIndex];
                RenderComment(sheet, dataRange, commentInfo[0], commentInfo[1], commentInfo[3]);
            }
        }

        var subScopes = scope.subScopes;
        if (subScopes) {
            for (var subScopeIndex = 0, subScopeCount = subScopes.length;
                 subScopeIndex < subScopeCount;
                 subScopeIndex++) {
                var subScope = subScopes[subScopeIndex];
                var subSheet =
                    scope.isTopInSheet
                        ? SpreadsheetApp.getActiveSpreadsheet().getSheetByName(subScope.sheetName)
                        : sheet;
                RenderScope(subSheet, subScope);
            }
        }

    }

    // borders
    //   top left bottom right vertical horizontal
    //   color
    //   style
    //     dotted dashed solid solid_medium solid_thick double
    // background color
    // font color
    // font family
    // font size
    // font weigh
    //   bold normal
    // font style
    //   italic normal
    // font line
    //   underline line-through none
    // horizontal alignment
    //   left center right
    // vertical alignment
    //   top middle bottom
    // text direction
    //   left_to_right right_to_left
    // text rotation
    // vertical text
    // wrap
    // wrap strategy
    //   wrap overflow clip
    // note
    // protected
    // data validation
    // number format


    var gObjectKeyBackgroundColors = [
        "#fff1ce", "#fee49d", "#fbe5ce", "#f8ca9f"
    ];
    var gObjectKeyHorizontalAlignment = 'right';
    var gObjectKeyFontFamily = 'Arial';

    function RenderObjectKey(sheet, dataRange, row, column, rows, columns, keyRow, keyColumn, key, index)
    {
        var range = sheet.getRange(keyRow + 1, keyColumn, rows, columns + 2);
        range.setHorizontalAlignment(gObjectKeyHorizontalAlignment);
        range.setFontFamily(gObjectKeyFontFamily);
        var color =
            gObjectKeyBackgroundColors[
                (index & 1) |
                ((column & 1) << 1)];
        range.setBackground(color);
    }


    var gArrayIndexBackgroundColors = [
      "#d0e3f2", "#a0c6e6", "#d9d2e8", "#b4a8d5"
    ];

    function RenderArrayIndex(sheet, dataRange, row, column, rows, columns, index)
    {
        var range = sheet.getRange(row + 1, column, rows, columns + 1);
        var color =
            gArrayIndexBackgroundColors[
                (index & 1) | 
                ((column & 1) << 1)];
        range.setBackground(color);
    }


    var gTypeColor = '#008000';
    var gTypeBackgroundColor = '#c0ffc0';
    var gTypeHorizontalAlignment = 'center';
    var gTypeFontFamily = 'Courier';
    var gTypeFontWeight = 'bold';
    var gTypeFontBorderStyle = SpreadsheetApp.BorderStyle.SOLID_MEDIUM;

    function RenderType(sheet, dataRange, row, column, type)
    {
        var range = sheet.getRange(row + 1, column + 1);
        range.setHorizontalAlignment(gTypeHorizontalAlignment);
        range.setBackground(gTypeBackgroundColor);
        range.setFontFamily(gTypeFontFamily);
        range.setFontWeight(gTypeFontWeight);
        range.setBorder(true, true, true, true, false, false, gTypeColor, gTypeFontBorderStyle);
    }


    var gValueColor = '#000000';
    var gValueFontFamily = 'Arial';
    var gValueHorizontalAlignment = 'left';

    function RenderValue(sheet, dataRange, row, column, rows, columns)
    {
        if ((rows > 0) && (columns > 0)) {
            var range = sheet.getRange(row + 1, column + 1, rows, columns);
            range.setFontFamily(gValueFontFamily);
            range.setHorizontalAlignment(gValueHorizontalAlignment);
        }
    }


    var gParamColor = '#000000';
    var gParamFontFamily = 'Arial';
    var gParamHorizontalAlignment = 'left';

    function RenderParams(sheet, dataRange, row, column, rows, columns)
    {
        if ((rows > 0) && (columns > 0)) {
            var range = sheet.getRange(row + 1, column + 1, rows, columns);
            range.setFontFamily(gParamFontFamily);
            range.setHorizontalAlignment(gParamHorizontalAlignment);
        }
    }


    var gCommentColor = '#0000ff';
    var gCommentFontFamily = 'Comic Sans MS';
    var gCommentFontStyle = 'italic';
    var gCommentLeftBorderStyle = SpreadsheetApp.BorderStyle.DOUBLE;
    var gCommentHorizontalAlignment = 'left';

    function RenderComment(sheet, dataRange, row, column, comment)
    {
        var range = sheet.getRange(row + 1, column + 1);
        range.setFontFamily(gCommentFontFamily);
        range.setFontStyle(gCommentFontStyle);
        range.setFontColor(gCommentColor);
        range.setBorder(false, true, false, false, false, false, gCommentColor, gCommentLeftBorderStyle);
        range.setHorizontalAlignment(gCommentHorizontalAlignment);
    }


    function RenderChildRows(sheet, dataRange, firstChildRow, lastChildRow)
    {
        if (lastChildRow == firstChildRow) {
            return;
        }
        var range = sheet.getRange(firstChildRow + 1, 1, lastChildRow - firstChildRow, 1);
        range.shiftRowGroupDepth(1);
    }


    function GetSheetValues(sheets, sheetName)
    {
        //console.log("GetSheet: sheets: " + Object.keys(sheets) + " sheetName: " + sheetName);

        var sheetData = 
            sheets[sheetName];
        if (sheetData) {
            return sheetData.values;
        }

        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var sheet = spreadsheet.getSheetByName(sheetName);

        if (!sheet) {
            console.log("sheets.js: GetSheetValues: can't find sheetName: " + sheetName);
            return null;
        }

        var spreadsheetID = spreadsheet.getId();
        var sheetID = sheet.getSheetId();
        var dataRange = sheet.getDataRange();
        var rows = dataRange.getNumRows();
        var columns = dataRange.getNumColumns();
        var values = dataRange.getValues();

        // Make sure the rows are all filled!
        var nullString = "";
        for (var row = 0; row < rows; row++) {
            var columnValues = values[row];
            while (columnValues.length < columns) {
                columnValues.push(nullString);
            }
        }

        sheetData = {
            sheetName: sheetName,
            sheetID: sheetID,
            spreadsheetID: spreadsheetID,
            rows: rows,
            columns: columns,
            values: values
        };

        sheets[sheetName] = sheetData;

        return sheetData.values;
    }


    function MakeNamedRanges(sheet)
    {
        console.log("sheets.js: MakeNamedRanges: sheet:", sheet);

        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        if (!sheet) {
            sheet = spreadsheet.getActiveSheet();
        }

        var sheetName = sheet.getName();
        var sheets = {};
        var rangeSchemas = LoadSheet(sheets, "rangeSchemas");
        if (!rangeSchemas) {
            console.log("sheets.js: MakeNamedRanges: Error loading rangeSchemas sheet.");
            return;
        }
        //console.log("sheets.js: MakeNamedRanges: got rangeSchemas:", JSON.stringify(rangeSchemas));

        var headers = {};
        rangeSchemas.forEach((rangeSchema) => {
            var header = rangeSchema.name + ':';
            var parts = headers[header];
            if (!parts) {
                parts = [];
                headers[header] = parts;
            }
            parts.push(rangeSchema);
        });

        console.log("headers:", JSON.stringify(headers));

        var fullRange = 
            sheet.getRange(1, 1, sheet.getMaxRows(), sheet.getMaxColumns());
        //ClearFormatting(sheet, fullRange);
        //fullRange.setNote(null);

        var namedRanges = spreadsheet.getNamedRanges();
        namedRanges.forEach((namedRange) => {
            var rangeSheetName = namedRange.getRange().getSheet().getName();
            if ((sheetName == rangeSheetName) ||
                (rangeSheetName.indexOf(sheetName + '$') == 0)) { // startsWith is missing!
                namedRange.remove();
            }
        });

        var dataRange = sheet.getDataRange();
        var rows = dataRange.getNumRows();
        var columns = dataRange.getNumColumns();
        var values = dataRange.getValues();
        var columnToRowColumns = {};
        var rowToColumnRows = {};
        var name = 'Unknown';

        for (var row = 0; row < rows; row++) {
            for (var column = 0; column < columns; column++) {

                var header = values[row][column];
                var parts = headers[header];
                if (!parts) {
                    continue;
                }

                var headerRange = sheet.getRange(row + 1, column + 1, 1, 1);
                //headerRange.setHorizontalAlignment('right');
                //headerRange.setFontWeight('bold');

                console.log("header", header, "row", row, "rows", rows, "columns", columns, "column", column, "parts", parts);

                parts.forEach((part, partIndex) => {

                    // part:
                    //     name string
                    //     rowOffset number
                    //     columnOffset number
                    //     layer string
                    //     type string
                    //     suffix string

                    var rangeRow = row + part.rowOffset;
                    var rangeColumn = column + part.columnOffset;

                    if (part.suffix == 'Name') {
                        name = values[rangeRow][rangeColumn];
                    }

                    var rangeName = name + '_' + part.suffix;

                   console.log("part", JSON.stringify(part), "rangeRow", rangeRow, "rangeColumn", rangeColumn, "name", name, "rangeName", rangeName);

                    //console.log("header", header, "rangeName", rangeName, "rowOffset", part.rowOffset, "columnOffset", part.columnOffset, "type", part.type, "layer", part.layer, "suffix", part.suffix, "row", row, "column", column, "rangeRow", rangeRow, "rangeColumn", rangeColumn, "rowToColumnRows[rangeRow]", rowToColumnRows[rangeRow], "columnToRowColumns[rangeColumn]", columnToRowColumns[rangeColumn]);

                    if ((part.type == 'cell') || (part.type == 'column')) {

                        columnToRowColumns[rangeColumn] = 1;

                        if ((part.type == 'column') &&
                            !rowToColumnRows[rangeRow]) {
                            for (var columnRows = 0, maxRows = rows - rangeRow; columnRows < maxRows; columnRows++) {
                                var value = values[rangeRow + columnRows][rangeColumn];
                                //console.log("SCAN", rangeRow + columnRows, rangeColumn, value);
                                if (!value) {
                                    break;
                                }
                            }

                            rowToColumnRows[rangeRow] = columnRows;
                            //console.log("SET rowToColumnRows", "rangeRow", rangeRow, "columnRows", columnRows);

                        }

                    }

                    if ((part.type == 'cell') || (part.type == 'row')) {

                        rowToColumnRows[rangeRow] = 1;

                        if ((part.type == 'row') &&
                            !columnToRowColumns[rangeColumn]) {
                            for (var rowColumns = 0, maxColumns = columns - rangeColumn; rowColumns < maxColumns; rowColumns++) {
                                var value = values[rangeRow][rangeColumn + rowColumns];
                                //console.log("SCAN", rangeRow, rangeColumn + rowColumns, value);
                                if (!value) {
                                    break;
                                }
                            }

                            columnToRowColumns[rangeColumn] = rowColumns;
                            //console.log("SET columnToRowColumns", "rangeColumn", rangeColumn, "rowColumns", rowColumns);

                        }

                    }

                    var rangeRows =
                        ((part.type == 'cell') || (part.type == 'row'))
                            ? 1
                            : rowToColumnRows[rangeRow] || 1;

                    var rangeColumns =
                        ((part.type == 'cell') || (part.type == 'column'))
                            ? 1
                            : columnToRowColumns[rangeColumn] || 1;

                    var layerSheet =
                        (part.layer == '')
                            ? sheet
                            : spreadsheet.getSheetByName(sheetName + '$' + part.layer);

                    if (!layerSheet) {
                        console.log("could not find sheetName", sheetName, "layer", part.layer);
                        return;
                    }

                    var range = layerSheet.getRange(rangeRow + 1, rangeColumn + 1, rangeRows, rangeColumns);
/*
                    if (sheet == layerSheet) {
                        range.setBackgroundColor('#c0c0ff');
                        range.setBorder(true, true, true, true, false, false, '#000000', SpreadsheetApp.BorderStyle.SOLID_MEDIUM);
                        //var note = 
                        //    rangeName + ':\nrow ' + rangeRow + ' column: ' + rangeColumn + 
                        //    '\nrows: ' + rangeRows + ' columns: ' + rangeColumns;
                        //range.setNote(note)
                    }
*/
                    console.log("sheets.js: MakeNamedRanges: setNamedRange: rangeName", rangeName, "rangeRow", rangeRow, "rangeColumn", rangeColumn, "rangeRows", rangeRows, "rangeColumns", rangeColumns, "layer", part.layer);

                    spreadsheet.setNamedRange(rangeName, range);

                });
            }
        }

        //UpdateMetaData();

    }


    function UpdateAllMetaData()
    {
        console.log("UpdateAllMetaData");

        MakeAllMetaDataSheets();
    }


    function MakeAllMetaDataSheets()
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();

        var sheets = spreadsheet.getSheets();

        for (var index = 0, n = sheets.length; index < n; index++) {
            var sheet = sheets[index];
            var sheetName = sheet.getName();
            console.log("MakeAllMetaDataSheets: sheetName:", sheetName);
            if (sheetName.indexOf('$') != -1) {
                continue;
            }

            MakeMetaDataSheets(sheet);
        }
    }


    function MakeMetaDataSheets(sheet)
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        if (!sheet) {
            sheet = spreadsheet.getActiveSheet();
        }
        var sheetName = sheet.getName();
        var sheetRows = sheet.getLastRow();
        var sheetColumns = sheet.getLastColumn();
        var values = sheet.getSheetValues(1, 1, sheetRows, sheetColumns);
        var layers = null;

        for (var row = 0; !layers && (row < sheetRows); row++) {
            for (var column = 0; !layers && (column < sheetColumns); column++) {
                var value = values[row][column];
                if (value == 'Layers:') {
                    value = values[row][column + 1] || '';
                    console.log("MakeMetaDataSheets: sheetName:", sheetName, "row:", row, "column:", column, "Layers:", value);
                    value.split(',').forEach((token) => {
                        token = token.trim();
                        if (token.length > 0) {
                            if (layers === null) {
                                layers = [];
                            }
                            if (layers.indexOf(token) == -1) {
                                layers.push(token);
                            }
                        }
                    });
                }
            }
        }

        if (layers) {
            console.log("MakeMetaDataSheets: sheetName:", sheetName, "layers:", layers);
            var lastActiveSheet = spreadsheet.getActiveSheet();
            layers.forEach((layer) => {
                if (layer == "namedRange") {
                    MakeNamedRanges(sheet);
                } else {
                    var layerSheetName = sheetName + '$' + layer;
                    var layerSheet = spreadsheet.getSheetByName(layerSheetName);
                    if (layerSheet) {
                        console.log("MakeMetaDataSheets: sheetName:", sheetName, "found layerSheetName:", layerSheetName, layerSheet);
                    } else {
                        console.log("MakeMetaDataSheets: sheetName:", sheetName, "making layerSheetName:", layerSheetName);
                        var nowSheets = spreadsheet.getSheets();
                        for (var insertSheetIndex = nowSheets.length - 1; insertSheetIndex > 0; insertSheetIndex--) {
                            var nowSheetName = nowSheets[insertSheetIndex].getName();
                            console.log("MakeMetaDataSheets: sheetName:", sheetName, "layerSheetName:", layerSheetName, "insertSheetIndex:", insertSheetIndex, "nowSheetName:", nowSheetName);
                            if ((nowSheetName == sheetName) ||
                                nowSheetName.indexOf(sheetName + '$') == 0) { // startsWith is missing!
                                insertSheetIndex++;
                                break;
                            }
                        }
                        console.log("MakeMetaDataSheets: sheetName:", sheetName, "layerSheetName:", layerSheetName, "insertSheetIndex:", insertSheetIndex);
                        spreadsheet.insertSheet(layerSheetName, insertSheetIndex);
                        layerSheet = spreadsheet.getActiveSheet();
                    }
                    layerSheet.clear();
                    var topLeftRange = layerSheet.getRange(1, 1, 1, 1);
                    var formula =
                        '=ARRAYFORMULA(GenerateLayerSheet(' +
                        JSON.stringify(sheetName) +
                        ', ' +
                        JSON.stringify(layer) +
                        ', ' +
                        Math.random() + 
                        '))';
                    topLeftRange.setFormula(formula);
                    console.log("MakeMetaDataSheets: sheetName:", sheetName, "layerSheetName:", layerSheetName, "formula:", formula);
                }
            });
            spreadsheet.setActiveSheet(lastActiveSheet);

            UpdateRanges();
        }
    }


    // =ARRAYFORMULA(GenerateLayerSheet("SheetName", "layer", Math.random()))
    function GenerateLayerSheet(sheetName, layer, trigger)
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var sheet = spreadsheet.getSheetByName(sheetName);
        if (!sheet) {
            return [["GenerateLayerSheet", sheetName, layer, "invalid sheetName"]];
        }

        var sheetRows = sheet.getLastRow();
        var sheetColumns = sheet.getLastColumn();
        var range = sheet.getRange(1, 1, sheetRows, sheetColumns);

        switch (layer) {

            case 'backgroundColor':
                return range.getBackgrounds();
                break;

            case 'fontColor':
                return range.getFontColors();
                break;

            case 'fontFamily':
                return range.getFontFamilies();
                break;

            case 'fontSize':
                return range.getFontSizes();
                break;

            case 'fontWeight':
                return range.getFontWeights();
                break;

            case 'fontStyle':
                return range.getFontStyles();
                break;

            case 'fontLine':
                return range.getFontLines();
                break;

            case 'note':
                return range.getNotes();
                break;

            case 'image':
                var results = [];
                for (var row = 0; row < sheetRows; row++) {
                    var rowValues = [];
                    results.push(rowValues);
                    for (var column = 0; column < sheetColumns; column++) {
                        rowValues.push('');
                    }
                }
                var images = sheet.getImages();
                images.forEach((image) => {
                    var anchorCell = image.getAnchorCell();
                    var row = anchorCell.getRow();
                    var column = anchorCell.getColumn();
                    var data = {
                        //keys: Object.keys(image),
                        //string: image.toString(),
                        sheet: sheetName,
                        row: row,
                        column: column,
                        xOffset: image.getAnchorCellXOffset(),
                        yOffset: image.getAnchorCellYOffset(),
                        altTextTitle: image.getAltTextTitle(),
                        altTextDescription: image.getAltTextDescription(),
                        width: image.getWidth(),
                        height: image.getHeight(),
                        inherentWidth: image.getInherentWidth(),
                        inherentHeight: image.getInherentHeight(),
                        script: image.getScript(),
                        url: image.getUrl() // Google BUG: url is always empty, dammit!
                    };

                    var dataList = [];
                    var jsonOld = results[row][column];
                    if (jsonOld != "") {
                        dataList = JSON.parse(jsonOld);
                    }
                    dataList.push(data);
                    var json = JSON.stringify(dataList);
                    console.log("GenerateLayerSheet:", "sheetName:", sheetName, "layer:", layer, "row:", row, "column:", column, "image:", image, "json:", json);
                    results[row][column] = json;
                });
                return results;

            default:
                return [["GenerateLayerSheet", sheetName, layer, "invalid layer"]];
                break;

        }
    }


    function UpdateRanges()
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var rangesSheet = spreadsheet.getSheetByName("ranges");
        if (!rangesSheet) {
            var lastActiveSheet = spreadsheet.getActiveSheet();
            spreadsheet.insertSheet("ranges");
            rangesSheet = spreadsheet.getActiveSheet();
            spreadsheet.setActiveSheet(lastActiveSheet);
        }
        rangesSheet.clear();
        var topLeftRange = rangesSheet.getRange(1, 1, 1, 1);
        var formula =
            '=ARRAYFORMULA(GenerateNamedRangeSheet(' +
            Math.random() +
            '))';
        topLeftRange.setFormula(formula);
        console.log("Updated Named Ranges", rangesSheet);
    }


    // =ARRAYFORMULA(GenerateNamedRangeSheet(Math.random()))
    function GenerateNamedRangeSheet(trigger)
    {
        var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
        var namedRanges = spreadsheet.getNamedRanges();
        var result = [];

        for (var index = 0, n = namedRanges.length; index < n; index++) {

            var namedRange = namedRanges[index];
            var rangeName = namedRange.getName();
            var range = namedRange.getRange();
            var sheet = range.getSheet();
            var sheetName = sheet.getName();
            var sheetID = sheet.getSheetId();
            var row = range.getRow();
            var column = range.getColumn();
            var rows = range.getNumRows();
            var columns = range.getNumColumns();

            result.push([
                "", rangeName, sheetName, sheetID, row, column, rows, columns
            ]);

        }

        result.sort(
            (a, b) => (a[1] < b[1]) ? -1 : ((a[1] > b[1]) ? 1 : 0));

        result.unshift(
            ["table"],
            ["", "{ rangeName string", "sheetName string", "sheetID string", "row number", "column number", "rows number", "columns number }" ]);

        return result;
    }


    function LoadSheet(sheets, sheetName)
    {
/*
        var values = GetSheetValues(sheets, sheetName);
        if (!values) {
            console.log("sheets.js: LoadSheet: Error getting sheet values. sheetName:", sheetName);
            return null;
        }
        return values;
*/
        var scope = SheetToScope(sheets, {}, sheetName);
        if (scope.error) {
            console.log("sheets.js: LoadSheet: Error parsing sheet. Error in sheet name:", scope.errorScope.errorSheetName, "row:", scope.errorScope.errorRow, "column:", scope.errorScope.errorColumn, "error:", scope.error, "errorScope:", scope.errorScope);
            return null;
        }
        return scope.value;
    }


    ////////////////////////////////////////////////////////////////////////


} else {


    ////////////////////////////////////////////////////////////////////////
    // Browser


    function LoadSheets(sheetSpecs, live, success, error)
    {
        var calledError = false;
        var xhrs = {};
        var data = {
            spreadsheetName: "Untitled",
            sheets: {},
            ranges: {}
        };

        for (var index = sheetSpecs.length - 1; index >= 0; index--) {

            ((index) => {

                var sheetSpec = sheetSpecs[index];
                var name = sheetSpec[0];
                if (xhrs[sheetSpec.sheetName]) {
                    return;
                }

                var url = GetSheetURL(sheetSpec, live);

                var xhr = new XMLHttpRequest();
                xhrs[sheetSpec.sheetName] = xhr;

                //console.log("sheets.js: LoadSheets: sheetName: " + sheetSpec.sheetName + " url: " + url);

                xhr.onload = () => {
                    var text = xhr.responseText;

                    var sheet = ParseTSVToSheet(text);

                    var rows = sheet.length;
                    var columns = 0;
                    for (var row = 0; row < rows; row++) {
                        var cols = sheet[row].length;
                        if (cols > columns) {
                            columns = cols;
                        }
                    }

                    data.sheets[sheetSpec.sheetName] = {
                        sheetName: sheetSpec.sheetName,
                        spreadsheetID: sheetSpec.spreadsheetID,
                        sheetID: sheetSpec.sheetID,
                        index: index,
                        rows: rows,
                        columns: columns,
                        values: sheet
                    };

                    delete xhrs[sheetSpec.sheetName];

                    var sheetsLeft = Object.keys(xhrs).length;

                    //console.log("sheets.js: LoadSheets: onload: Loaded sheet name: " + sheetSpec.sheetName + " sheetsLeft: " + sheetsLeft);

                    if (sheetsLeft === 0) {

                        // If there was a sheet called ranges, then load the ranges from it.
                        if (data.sheets.ranges) {
                            var scope = SheetToScope(data.sheets, {}, 'ranges');
                            var rangesTable = scope.value;

                            if (scope.error) {
                                console.log("sheets.js: LoadSheets: Error loading ranges. Error in sheet name:", scope.errorScope.errorSheetName, "row:", scope.errorScope.errorRow, "column:", scope.errorScope.errorColumn, "error:", error, "errorScope:", scope.errorScope);
                            } else if (!rangesTable) {
                                console.log("sheets.js: LoadSheets: Loaded ranges but it was null.", "scope:", scope);
                                if (!calledError) {
                                    calledError = true;
                                    error();
                                }
                                return;
                            } else {
                                // Add each row of the range table to the range map by its rangeName.
                                for (var i = 0, n = rangesTable.length; i < n; i++) {
                                    var rangeRow = rangesTable[i];
                                    data.ranges[rangeRow.rangeName] = rangeRow;
                                }
                                console.log("sheets.js: LoadObjects: LoadSheetsSuccess: Loaded ranges:", data.ranges, "scope:", scope);
                            }

                        }

                        success(data);
                    }

                };

                xhr.onerror = (progressEvent) => {
                    console.log("sheets.js: LoadSheets: error loading sheet name: " + name + " url:", url, "xhr:", xhr, "REPLIED:", xhr.statusText, "progressEvent:", progressEvent);
                    if (!calledError) {
                        calledError = true;
                        error();
                    }
                };

                xhr.open('GET', url);
                xhr.send();

            })(index);

        }

    }


    function LoadSheetsFromApp(appURL, success, error)
    {
        var url = appURL;

        var xhr = new XMLHttpRequest();

        console.log("sheets.js: LoadSheetsFromApp: url: " + url);

        xhr.onload = () => {
            var text = xhr.responseText;
            var result = JSON.parse(text);
            if (result.status != 'success') {
                console.log("sheets.js: LoadSheetsFromApp: Invalid status: " + text);
                error();
                return;
            }

            success(result.data);
        };

        xhr.onerror = (err) => {
            console.log("sheets.js: LoadSheets: error loading url: " + url, "xhr:", xhr, "REPLIED:", xhr.statusText, "err:", err);
            error();
        };

        xhr.open('GET', url);
        xhr.send();
    }


    function GetSheetURL(sheetSpec, live)
    {
        var sheetURL;

        if (live) {

            sheetURL =
                'https://docs.google.com/spreadsheets/d/' +
                sheetSpec.spreadsheetID + 
                '/export?format=tsv&id=' +
                sheetSpec.spreadsheetID +
                '&gid=' +
                sheetSpec.sheetID;
            //console.log("LIVE sheetURL", sheetURL);

            // Always use the proxy for now.
            var proxyPrefix = 'https://struct.studio/_p/miniProxy.php';

            //console.log("Loading live spreadsheets from sheetURL:", sheetURL, "proxyPrefix:", params.proxyPrefix);

            if (proxyPrefix) {
                sheetURL = proxyPrefix + '?' + sheetURL;
                //console.log("PROXY sheetURL", sheetURL);
            }

        } else {

            sheetURL = 
                gStreamingAssetsAppURL + 
                'sheets/' + 
                sheetSpec.sheetFileName;

            //console.log("Loading baked-in spreadsheets from sheetURL:", sheetURL);

            //console.log("BAKED sheetURL", sheetURL);

        }

        return sheetURL;
    }


    function ParseTSVToSheet(text)
    {
        text = text.replace(/\r\n|\r/g, '\n');
        var textRows = text.split('\n');
        var sheet = [];
        for (var rowIndex = 0, rowCount = textRows.length; rowIndex < rowCount; rowIndex++) {
            var textRow = textRows[rowIndex];
            var textColumns = textRow.split('\t');
            var columns = [];
            sheet.push(columns);
            for (var columnIndex = 0, columnCount = textColumns.length; columnIndex < columnCount; columnIndex++) {
                var textCell = textColumns[columnIndex];
                columns.push(textCell);
            }
        }
        return sheet;
    }


    function GetSheetValues(sheets, sheetName)
    {
        var sheetData = 
            sheets[sheetName];
        if (sheetData) {
            return sheetData.values;
        }

        console.log("GetSheetValues: undefined sheetName: " + sheetName);

        return null;
    }


    ////////////////////////////////////////////////////////////////////////

}


////////////////////////////////////////////////////////////////////////
// Shared


function SheetToScope(sheets, ranges, sheetName, isSingleSheet)
{
    if (!sheetName || sheetName.length == 0) {
        return null;
    }

    var sheet = GetSheetValues(sheets, sheetName);

    if (!sheet || sheet.length == 0) {
        return null;
    }

    var scope = {
        sheetName: sheetName,
        sheet: sheet,
        row: 0,
        column: 0,
        rowCount: sheet.length,
        columnCount: sheet[0].length,
        isTopInSheet: true,
        isSingleSheet: !!isSingleSheet
    };

    scope.errorScope =
        LoadJSONFromSheet(sheets, ranges, scope);

    if (scope.errorScope) {
        if (scope.errorScope.error) {
            scope.error = scope.errorScope.error;
            scope.errorSheetName = scope.errorScope.errorSheetName;
            scope.errorRow = scope.errorScope.errorRow;
            scope.errorColumn = scope.errorScope.errorColumn;
        }
    }

    return scope;
}


// Recursively loads JSON from a spreadsheet.
// The scope is an object with the following input keys:
//   sheetName: name of the sheet
//   sheet: the spreadsheet, an array of arrays of strings
//   row: the row to read from
//   column: the column to read from
//   rowCount: the number of rows to consider
//   columnCount: the number of columns to consider
// The scope will be returned with the following output keys:
//   value: the value if there was no error
//   rowsUsed: the number of rows used
//   error: a string error message if there was an error
//   errorScope: the scope containing the error
// Returns null on success, or a scope with an error.
// The location of the error in the spreadsheet can be determined by the row and column of the errorScope.
// The scopes are enriched with optional metadata used to provide feedback by formatting the spreadsheet, etc.

function LoadJSONFromSheet(sheets, ranges, scope)
{
    var i;
    var n;
    var key;
    var index;
    var unindented;
    var subScope;
    var stripNumberRegexp = new RegExp('[,$ ]', 'g');


    scope.currentRow = scope.row;
    scope.currentColumn = scope.column;
    scope.gotValue = true;
    scope.valueRow = scope.row;
    scope.valueColumn = scope.column;
    scope.valueRows = 1;
    scope.valueColumns = 1;
    scope.rowValues = scope.sheet[scope.currentRow];
    scope.value = null;
    scope.rowsUsed = 1;
    scope.comments = [];


    function ConvertToType(str, type)
    {
        switch (type) {

            case "null":
                return null;

            case "boolean":
                return ('' + str).trim().toLowerCase() == "true";

            case "string":
                return "" + str;

            case "number":
            case "float":
                str = ('' + str).trim().replace(stripNumberRegexp, '');
                return (str === '') ? 0 : parseFloat(str);

            case "int":
                str = ('' + str).trim().replace(stripNumberRegexp, '');
                return (str === '') ? 0 : parseInt(str);

            case "json":
                try {
                    return JSON.parse(str);
                } catch (e) {
                    console.log("sheets.js: JSON parsing error!", e, str);
                }
                return null;

            default:
                console.log("sheets.js: ConvertToType: invalid type: " + type);
                return null;

        }

    }


    function GetColumnString(column, doNotTrim)
    {
        if (isNaN(column) || 
            ((column != "") && 
             (!column))) {
            var oops = true;
            return "";
        }

        var result = scope.rowValues[column];
        if (!result) {
            return "";
        }

        result = "" + result;

        if (!doNotTrim) {
            result = result.trim();
        }

        return result;
    }


    function GetColumnNumber(column)
    {
        var str = GetColumnString(column);

        return ConvertToType(str, "number");
    }


    function GetColumnInteger(column)
    {
        var str = GetColumnString(column);

        return ConvertToType(str, "integer");
    }


    function FindComments(row, column, columns)
    {
        var rowValues =
            scope.sheet[row];
        var end =
            columns
                ? (column + columns)
                : scope.rowValues.length;
        for (var col = column; col < end; col++) {
            var comment = rowValues[col];
            // Zero is not a comment!
            if ((comment !== "") &&
                (comment !== null)) {
                scope.comments.push([row, col, comment]);
            }
        }
    }


    // If a type was not passed in (not single cell), then pick up the type from
    // the cell in the spreadsheet and move to the next cell to the right.
    if (!scope.isSingleCell) {
        scope.gotType = true;
        scope.typeName = GetColumnString(scope.currentColumn);
        scope.typeRow = scope.currentRow;
        scope.typeColumn = scope.currentColumn;
        scope.valueColumn++;
        if (!scope.typeName) {
            scope.error = "Expected a typeName.";
            scope.errorSheetName = scope.sheetName;
            scope.errorRow = scope.currentRow;
            scope.errorColumn = scope.currentColumn;
            return scope; // error
        }
    }

    //console.log("LoadJSONFromSheet: row " + scope.row + " col " + scope.col + " rowCount " + scope.rowCount + " colCount " + scope.colCount + " typeName " + scope.typeName + " currentRow " + scope.currentRow + " currentColumn " + scope.currentColumn + " value " + value);

    switch (scope.typeName) {

        case "null":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn);
            }
            scope.value = null;
            scope.valueColumn = -1;
            scope.valueRow = -1;
            scope.valueRows = 0;
            scope.valueColumns = 0;
            return null; // success

        case "boolean":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = ConvertToType(GetColumnString(scope.valueColumn), "boolean");
            return null; // success

        case "booleans":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = ConvertToType(GetColumnString(scope.valueColumn), "boolean");
            if (scope.isSingleCell) {
                scope.error = "Type booleans can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }
            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 1;
            n = GetColumnInteger(scope.valueColumn);
            scope.valueColumn++;
            scope.valueColumns = n;
            scope.value = [];
            for (i = 0; i < n; i++) {
                scope.value.push(GetColumnString(scope.valueColumn + i).toLowerCase() == "true");
            }
            return null; // success

        case "string":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = GetColumnString(scope.valueColumn, true);
            return null; // success

        case "strings":
            if (scope.isSingleCell) {
                scope.error = "Type strings can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }
            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 1;
            n = GetColumnInteger(scope.valueColumn);
            scope.valueColumn++;
            scope.valueColumns = n;
            scope.value = [];
            for (i = 0; i < n; i++) {
                scope.value.push(GetColumnString(scope.valueColumn + i, true));
            }
            return null; // success

        case "number":
        case "float":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = GetColumnNumber(scope.valueColumn);
            return null; // success

        case "numbers":
        case "floats":
            if (scope.isSingleCell) {
                scope.error = "Type numbers can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }
            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 1;
            n = GetColumnInteger(scope.valueColumn);
            scope.valueColumn++;
            scope.valueColumns = n;
            scope.value = [];
            for (i = 0; i < n; i++) {
                scope.value.push(GetColumnNumber(scope.valueColumn + i));
            }
            return null; // success

        case "integer":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = GetColumnInteger(scope.valueColumn);
            return null; // success

        case "integers":
            if (scope.isSingleCell) {
                scope.error = "Type integers can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }
            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 1;
            n = GetColumnInteger(scope.valueColumn);
            scope.valueColumn++;
            scope.valueColumns = n;
            scope.value = [];
            for (i = 0; i < n; i++) {
                scope.value.push(GetColumnInteger(scope.valueColumn + i));
            }
            return null; // success

        case "json":
            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }
            scope.value = "" + GetColumnString(scope.valueColumn, true);
            try {
                scope.value = JSON.parse(scope.value);
            } catch (e) {
                console.log("sheets.js: JSON parsing error!", e, scope.value);
            }
            return null; // success

        case "object":

            if (scope.isSingleCell) {
                scope.error = "Type object can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }

            FindComments(scope.currentRow, scope.valueColumn);

            scope.value = {};
            scope.subScopes = [];
            scope.currentRow++;
            scope.hasChildRows = true;
            scope.firstChildRow = scope.currentRow;
            scope.lastRow = scope.row + scope.rowCount;
            scope.lastColumn = scope.column + scope.columnCount;
            index = 0;

            while (scope.currentRow < scope.lastRow) {
                scope.rowValues = scope.sheet[scope.currentRow];

                // If this row is unindented, then we are done.
                unindented = false;
                scope.previousColumn = scope.currentColumn - (scope.alreadyIndented ? 1 : 0);
                for (i = 0; i <= scope.previousColumn; i++) {
                    if (GetColumnString(i) != "") {
                        unindented = true;
                        break;
                    }
                }
                if (unindented) {
                    break;
                }

                scope.startColumn = scope.currentColumn + (scope.alreadyIndented ? 0 : 1);
                key = GetColumnString(scope.startColumn);
                if (key == "") {
                    FindComments(scope.currentRow, scope.startColumn + 1);
                    scope.currentRow++;
                } else {

                    subScope = {
                        parentScope: scope,
                        sheetName: scope.sheetName,
                        sheet: scope.sheet,
                        isSingleSheet: scope.isSingleSheet,
                        row: scope.currentRow,
                        column: scope.startColumn + 1,
                        rowCount: scope.lastRow - scope.currentRow,
                        columnCount: scope.lastColumn - scope.startColumn - 1,
                        alreadyIndented: true,
                        inObject: true,
                        index: index,
                        key: key,
                        keyRow: scope.currentRow,
                        keyColumn: scope.startColumn
                    };

                    scope.subScopes.push(subScope);
                    index++;

                    scope.errorScope = LoadJSONFromSheet(sheets, ranges, subScope);
                    if (scope.errorScope) {
                        return scope.errorScope; // error
                    }

                    scope.value[key] = subScope.value;
                    scope.currentRow += subScope.rowsUsed;

                    //console.log("SET OBJECT key " + key + " rowsUsed " + subScope.rowsUsed + " value " + JSON.stringify(subScope.value));
                }
            }

            scope.lastChildRow = scope.currentRow;

            // Return the number of rows actually used.
            scope.rowsUsed = scope.currentRow - scope.row;

            return null; // success

        case "array":

            if (scope.isSingleCell) {
                scope.error = "Type array can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }

            FindComments(scope.currentRow, scope.valueColumn);

            scope.value = [];
            scope.subScopes = [];
            scope.currentRow++;
            scope.hasChildRows = true;
            scope.firstChildRow = scope.currentRow;
            scope.lastRow = scope.row + scope.rowCount;
            scope.lastColumn = scope.column + scope.columnCount;
            index = 0;

            while (scope.currentRow < scope.lastRow) {
                scope.rowValues = scope.sheet[scope.currentRow];

                // If this row is unindented, then we are done. 
                unindented = false;
                scope.previousColumn = scope.currentColumn - (scope.alreadyIndented ? 1 : 0);
                for (i = 0; i <= scope.previousColumn; i++) {
                    if (GetColumnString(i) != "") {
                        unindented = true;
                        break;
                    }
                }
                if (unindented) {
                    break;
                }

                scope.startColumn = scope.currentColumn + (scope.alreadyIndented ? 0 : 1);
                var cell = GetColumnString(scope.startColumn);
                if (!cell) {
                    FindComments(scope.currentRow, scope.startColumn + 1);
                    scope.currentRow++;
                } else {

                    subScope = {
                        parentScope: scope,
                        sheetName: scope.sheetName,
                        sheet: scope.sheet,
                        isSingleSheet: scope.isSingleSheet,
                        row: scope.currentRow,
                        column: scope.startColumn,
                        rowCount: scope.lastRow - scope.currentRow,
                        columnCount: scope.lastColumn - scope.startColumn,
                        inArray: true,
                        index: index
                    };

                    scope.subScopes.push(subScope);

                    scope.errorScope = LoadJSONFromSheet(sheets, ranges, subScope);
                    if (scope.errorScope) {
                        return scope.errorScope; // error
                    }

                    scope.value.push(subScope.value);
                    index++;

                    scope.currentRow += subScope.rowsUsed;

                }

            }

            scope.lastChildRow = scope.currentRow;

            // Return the number of rows actually used.
            scope.rowsUsed = scope.currentRow - scope.row;

            return null; // success

        case "sheet":

            if (!scope.isSingleCell) {
                FindComments(scope.currentRow, scope.valueColumn + 1);
            }

            var sheetName = GetColumnString(scope.valueColumn);
            if (sheetName == "") {
                scope.error = "Expected 'sheet sheetName'.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope;
            }

            if (scope.isSingleSheet) {

                scope.value = null;

            } else {
  
                scope.sheetScope = SheetToScope(sheets, ranges, sheetName);
                if (!scope.sheetScope) {
                    scope.error = "Could not find sheet: " + sheetName;
                    scope.errorSheetName = scope.sheetName;
                    scope.errorRow = scope.currentRow;
                    scope.errorColumn = scope.currentColumn;
                    return scope;
                }
    
                scope.sheetScope.parentScope = scope;
                scope.subScopes = [scope.sheetScope];
    
                if (scope.sheetScope.error) {
                    scope.errorScope = scope.sheetScope;
                    return scope.errorScope; // error
                }
    
                scope.value = scope.sheetScope.value;
              
            }

            return null; // success

        case "grid":

            if (scope.isSingleCell) {
                scope.error = "Type grid can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }

            FindComments(scope.currentRow, scope.valueColumn + 3);

            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 3;

            var gridTypeName = GetColumnString(scope.valueColumn);
            if (gridTypeName) {
                gridTypeName = ("" + gridTypeName).trim();
            }
            if (!gridTypeName) {
                scope.error = "Expected 'grid typeName columns rows'.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn;
                return scope; // error
            }

            var gridColumns = GetColumnString(scope.valueColumn + 1);
            if (gridColumns) {
                gridColumns = parseInt(gridColumns);
            }
            if (!gridColumns || (gridColumns < 1)) {
                scope.error = "Expected 'grid typeName columns rows', missing columns > 0.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 1;
                return scope; // error
            }

            var gridRows = GetColumnString(scope.valueColumn + 2);
            if (gridRows) {
                gridRows = parseInt(gridRows);
            }
            if (!gridRows || (gridRows < 1)) {
                scope.error = "Expected 'grid typeName columns rows', missing rows > 0";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 2;
                return scope; // error
            }

            scope.value = [];
            scope.subScopes = [];
            scope.currentRow++;
            scope.hasChildRows = true;
            scope.firstChildRow = scope.currentRow;
            scope.lastChildRow = scope.currentRow + gridRows + 1;
            scope.valueRow = scope.currentRow;
            scope.valueColumn = scope.currentColumn;
            scope.valueRows = gridRows;
            scope.valueColumns = gridColumns;
            scope.startRow = scope.currentRow;
            scope.startColumn = scope.currentColumn;

            for (var gridRowIndex = 0; gridRowIndex < gridRows; gridRowIndex++) {

                var gridRow = scope.startRow + gridRowIndex;
                var gridRowValues = [];

                scope.value.push(gridRowValues);
                scope.rowValues = scope.sheet[gridRow];

                FindComments(gridRow, 0, scope.startColumn);
                FindComments(gridRow, scope.startColumn + gridColumns);

                for (var gridColumnIndex = 0; gridColumnIndex < gridColumns; gridColumnIndex++) {

                    var gridColumn = scope.startColumn + gridColumnIndex;

                    subScope = {
                        sheetName: scope.sheetName,
                        sheet: scope.sheet,
                        isSingleSheet: scope.isSingleSheet,
                        isSingleCell: true,
                        row: gridRow,
                        column: gridColumn,
                        rowCount: 1,
                        columnCount: 1,
                        typeName: gridTypeName,
                        alreadyIndented: true,
                        inGrid: true,
                        gridRow: gridRow,
                        gridColumn: gridColumn,
                        gridRowIndex: gridRowIndex,
                        gridColumnIndex: gridColumnIndex
                    };

                    scope.subScopes.push(subScope);

                    scope.errorScope = LoadJSONFromSheet(sheets, ranges, subScope);
                    if (scope.errorScope) {
                        return scope.errorScope; // error
                    }

                    gridRowValues.push(subScope.value);

                }

            }

            // Return the number of rows actually used.
            scope.rowsUsed += 1 + gridRows;

            return null; // success

        case "range":

            if (scope.isSingleCell) {
                scope.error = "Type range can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }

            FindComments(scope.currentRow, scope.valueColumn + 3);

            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = scope.valueColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = 3;

            var rangeTypeName = GetColumnString(scope.valueColumn);
            if (rangeTypeName) {
                rangeTypeName = ("" + rangeTypeName).trim();
            }
            if (!rangeTypeName) {
                scope.error = "Expected 'range typeName selector rangeName'.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn;
                return scope; // error
            }

            var rangeSelector = GetColumnString(scope.valueColumn + 1);
            if (rangeSelector) {
                rangeSelector = ("" + rangeSelector).trim();
            }
            if (!rangeSelector) {
                scope.error = "Expected 'range typeName selector rangeName'.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 1;
                return scope; // error
            }
            var tokens = rangeSelector && rangeSelector.split(' ');
            if (!tokens ||
                !tokens.length) {
                scope.error = "Invalid selector: " + rangeSelector;
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 1;
                return scope; // error
            }

            var rangeName = GetColumnString(scope.valueColumn + 2);
            if (rangeName) {
                rangeName = ("" + rangeName).trim();
            }
            if (!rangeName) {
                scope.error = "Expected 'range typeName selector rangeName'.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 2;
                return scope; // error
            }
            var range = ranges[rangeName];
            if (!range) {
                scope.error = "Undefined range name: " + rangeName + " ranges: " + Object.keys(ranges);
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 2;
                return scope; // error
            }

            var values = GetSheetValues(sheets, range.sheetName);
            if (!values) {
                scope.error = "Range name: " + rangeName + " uses undefined sheet name: " + range.sheetName + " sheet names: " + Object.keys(sheets);
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 2;
                return scope; // error
            }

            scope.value = [];
            
            var expectedTokens = 1;
            switch (tokens[0]) {

                case 'all':
                    for (var row = 0; row < range.rows; row++) {
                        var rowValues = values[range.row + row - 1];
                        var dataRow = [];
                        scope.value.push(dataRow);
                        for (var column = 0; column < range.columns; column++) {
                            var str = "";
                            if (rowValues) {
                                var c = range.column + column - 1;
                                str = rowValues[c] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            dataRow.push(value);
                        }
                    }
                    break;

                case 'transpose':
                    for (var column = 0; column < range.columns; column++) {
                        var dataRow = [];
                        scope.value.push(dataRow);
                        for (var row = 0; row < range.rows; row++) {
                            var rowValues = values[range.row + row - 1];
                            var str = "";
                            if (rowValues) {
                                str = rowValues[range.column + column - 1] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            dataRow.push(value);
                        }
                    }
                    break;

                case 'columnMajor':
                    for (var row = 0; row < range.rows; row++) {
                        var rowValues = values[range.row + row - 1];
                        var dataRow = [];
                        scope.value.push(dataRow);
                        for (var column = 0; column < range.columns; column++) {
                            var str = "";
                            if (rowValues) {
                                str = rowValues[range.column + column - 1] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            scope.value.push(value);
                        }
                    }
                    break;

                case 'rowMajor':
                    for (var column = 0; column < range.columns; column++) {
                        var dataRow = [];
                        data.value.push(dataRow);
                        for (var row = 0; row < range.rows; row++) {
                            var rowValues = values[range.row + row - 1];
                            var str = "";
                            if (rowValues) {
                                str = rowValues[range.column + column - 1] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            scope.value.push(value);
                        }
                    }
                    break;

                case 'row':
                    if (tokens.length == 1) {
                        scope.value = range.row;
                    } else if (tokens.length == 2) {
                        var row = parseInt(tokens[1]);
                        var rowValues = values[range.row + row - 1];
                        for (var column = 0; column < range.columns; column++) {
                            var str = "";
                            if (rowValues) {
                                str = rowValues[range.column + column - 1] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            scope.value.push(value);
                        }
                        expectedTokens = 2;
                    } else {
                        expectedTokens = 2;
                    }
                    break;

                case 'column':
                    if (tokens.length == 1) {
                        scope.value = range.column;
                    } else if (tokens.length == 2) {
                        var column = parseInt(tokens[1]);
                        for (var row = 0; row < range.rows; row++) {
                            var rowValues = values[range.row + row - 1];
                            var str = "";
                            if (rowValues) {
                                str = rowValues[range.column + column - 1] || "";
                            }
                            var value = ConvertToType(str, rangeTypeName);
                            scope.value.push(value);
                        }
                        expectedTokens = 2;
                    } else {
                        expectedTokens = 2;
                    }
                    break;

                case 'cell':
                    expectedTokens = 3;
                    if (tokens.length == 3) {
                        var row = parseInt(tokens[1]);
                        var column = parseInt(tokens[2]);
                        var rowValues = values[range.row + row - 1];
                        var str = "";
                        if (rowValues) {
                            str = rowValues[range.column + column - 1] || "";
                        }
                        var value = ConvertToType(str, rangeTypeName);
                        scope.value = value;
                    }
                    break;

                case 'rows':
                    scope.value = range.rows;
                    break;

                case 'columns':
                    scope.value = range.columns;
                    break;

                case 'sheetName':
                    scope.value = range.sheetName;
                    break;

                case 'sheetID':
                    scope.value = range.sheetID;
                    break;

                default:
                    scope.error = "Invalid range selector: " + rangeSelector;
                    scope.errorSheetName = scope.sheetName;
                    scope.errorRow = scope.currentRow;
                    scope.errorColumn = scope.valueColumn + 1;
                    return scope; // error

            }

            if (expectedTokens != tokens.length) {
                scope.error = "Range name: " + rangeName + " uses invalid cell selector: " + rangeSelector;
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.valueColumn + 1;
                return scope; // error
            }

            //console.log("range", rangeTypeName, rangeSelector, rangeName, scope.value);

            return null; // success

        case "table":

            if (scope.isSingleCell) {
                scope.error = "Type table can't be used as a single cell.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope; // error
            }

            FindComments(scope.currentRow, scope.valueColumn);

            scope.value = [];
            scope.subScopes = [];

            scope.lastRow = scope.row + scope.rowCount;
            scope.lastColumn = scope.column + scope.columnCount;

            // Get the next indented row of headers.
            scope.currentRow++;

            // Make sure we are not at the end of the spreadsheet.
            if (scope.currentRow >= scope.lastRow) {
                scope.error = "Type table should be followed by a row of table headers.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = scope.currentColumn;
                return scope;
            }

            scope.rowValues = scope.sheet[scope.currentRow];

            var header;
            var headerColumn;
            var headerRow = scope.currentRow;

            // Make sure the next indented row of headers is not missing.
            scope.previousColumn = scope.currentColumn - (scope.alreadyIndented ? 1 : 0);
            for (headerColumn = 0; headerColumn <= scope.previousColumn; headerColumn++) {
                header = GetColumnString(headerColumn);
                if (header != "") {
                    scope.error = "Type table should be follow by an indented row of table headers, not an unindented row.";
                    scope.errorSheetName = scope.sheetName;
                    scope.errorRow = scope.currentRow;
                    scope.errorColumn = headerColumn;
                    return scope;
                }
            }

            // Make sure there are one or more indented headers.
            header = GetColumnString(headerColumn);

            if (!header) {
                scope.error = "Type table should be follow by an indented row of table headers. Missing the first header.";
                scope.errorSheetName = scope.sheetName;
                scope.errorRow = scope.currentRow;
                scope.errorColumn = headerColumn;
                return scope;
            }

            // Gather the headers, skipping columns with empty headers.
            var headers = [];
            var firstHeaderColumn = headerColumn;
            var lastHeaderColumn = -1;
            for (; headerColumn < scope.lastColumn; headerColumn++) {
                header = GetColumnString(headerColumn);
                if (header == "") {
                    continue;
                }
                headers.push([header, headerRow, headerColumn]);
                lastHeaderColumn = headerColumn;
            }

            var headerColumns = (lastHeaderColumn + 1) - firstHeaderColumn;
            scope.gotParams = true;
            scope.paramsRow = scope.currentRow;
            scope.paramsColumn = firstHeaderColumn;
            scope.paramsRows = 1;
            scope.paramsColumns = headerColumns;

            // Parse the column headers into tokens associated with columns.
            var tokens = [];
            scope.tokens = tokens;
            for (var headersIndex = 0, headersLength = headers.length; headersIndex < headersLength; headersIndex++) {

                var token = "";
                var headerInfo = headers[headersIndex];
                header = headerInfo[0];
                headerRow = headerInfo[1];
                headerColumn = headerInfo[2];

                function finishToken()
                {
                    if (token == "") {
                        return;
                    }
                    tokens.push([token, headerRow, headerColumn]);
                    token = "";
                }

                for (var headerIndex = 0, headerLength = header.length; headerIndex < headerLength; headerIndex++) {
                    var ch = header[headerIndex];

                    switch (ch) {

                        case '{':
                        case '}':
                        case '[':
                        case ']':
                            finishToken();
                            tokens.push([ch, headerRow, headerColumn]);
                            break;

                        case ' ':
                            finishToken();
                            break;

                        default:
                            token += ch;
                            break;

                    }

                }

                finishToken();

            }

            tokens.push(['.', -1, -1]);

            // Go to the next row and read in subsequent rows until the end of the spreadsheet or an unindented row.
            scope.currentRow++;
            index = 0;

            while (scope.currentRow < scope.lastRow) {

                scope.rowValues = scope.sheet[scope.currentRow];

                // If this row is unindented, then we are done.
                unindented = false;
                scope.previousColumn = scope.currentColumn - (scope.alreadyIndented ? 1 : 0);
                for (i = 0; i <= scope.previousColumn; i++) {
                    if (GetColumnString(i) != "") {
                        unindented = true;
                        break;
                    }
                }
                if (unindented) {
                    break;
                }

                scope.startColumn = scope.currentColumn + (scope.alreadyIndented ? 0 : 1);

                FindComments(scope.currentRow, 0, scope.startColumn);
                FindComments(scope.currentRow, lastHeaderColumn + 1);

                var tokenIndex = 0;
                var error = null;
                var errorRow = scope.currentRow;
                var errorColumn = scope.startColumn;
                var value = null;
                var valueStack = [];

                // Get the next tokenInfo, containing the token and the column in which it occurred.
                function NextTokenInfo()
                {
                    if (tokenIndex >= tokens.length) {
                        console.log("sheets.js: NextTokenInfo: Ran out of tokens!");
                        return ['.', -1, -1];
                    }
                    var tokenInfo = tokens[tokenIndex++];
                    return tokenInfo;
                }

                // Parse the top level structure, either an array or an object, into value.
                // Returns true if successful, false if not.
                // Sets value on success, or error on failure.
                function ParseTop()
                {
                    value = null;

                    var tokenInfo = NextTokenInfo();
                    var token = tokenInfo[0];

                    switch (token) {

                        case '{':
                            if (!ParseObject()) {
                                return false;
                            }
                            break;

                        case '[':
                            if (!ParseArray()) {
                                return false;
                            }
                            break;

                        default:
                            error = "ParseTop: expected initial '{' or '['.";
                            return false;

                    }

                    tokenInfo = NextTokenInfo();
                    token = tokenInfo[0];
                    if (token != '.') {
                        error = "ParseTop: did not expect any tokens after final '}' or ']'.";
                        errorRow = tokenRow;
                        errorColumn = tokenColumn;
                        return false;
                    }

                    return true;
                }

                // Parse an object into value.
                // Returns true if successful, false if not.
                // Sets value on success, or error on failure.
                function ParseObject()
                {
                    value = {};

                    while (true) {

                        var tokenInfo = NextTokenInfo();
                        var token = tokenInfo[0];
                        var tokenRow = tokenInfo[1];
                        var tokenColumn = tokenInfo[2];

                        switch (token) {

                            case '}':
                                return true;

                            case '{':
                            case '[':
                            case ']':
                            case '.':
                                error = "ParseObject: expected key of 'key type' or '}' instead of " + token;
                                errorRow = tokenRow;
                                errorColumn = tokenColumn;
                                return false;

                            default:

                                var key = token;
                                var typeTokenInfo = NextTokenInfo();
                                var typeToken = typeTokenInfo[0];
                                var typeRow = typeTokenInfo[1];
                                var typeColumn = typeTokenInfo[2];

                                switch (typeToken) {

                                    case '{':

                                        valueStack.push(value);

                                        if (!ParseObject()) {
                                            return false;
                                        }

                                        var objectValue = value;
                                        value = valueStack.pop();
                                        value[key] = objectValue;

                                        break;

                                    case '[':

                                        valueStack.push(value);

                                        if (!ParseArray()) {
                                            return false;
                                        }

                                        var arrayValue = value;
                                        value = valueStack.pop();
                                        value[key] = arrayValue;

                                        break;

                                    case '}':
                                    case ']':
                                    case '.':
                                        error = "ParseObject: expected type of 'key type' instead of " + token;
                                        errorRow = tokenRow;
                                        errorColumn = tokenColumn;
                                        return false;

                                    default:
                                        var typeName = typeToken;
                                        
                                        subScope = {
                                            sheetName: scope.sheetName,
                                            sheet: scope.sheet,
                                            isSingleSheet: scope.isSingleSheet,
                                            isSingleCell: true,
                                            row: scope.currentRow,
                                            column: typeColumn,
                                            rowCount: 1,
                                            columnCount: 1,
                                            typeName: typeName,
                                            alreadyIndented: true,
                                            inTable: true,
                                            index: index,
                                            tableRow: scope.currentRow,
                                            tableColumn: firstHeaderColumn,
                                            tableRows: 1,
                                            tableColumns: headerColumns,
                                            inTableObject: true,
                                            inTableObjectKey: key
                                        };

                                        scope.subScopes.push(subScope);

                                        scope.errorScope = LoadJSONFromSheet(sheets, ranges, subScope);
                                        if (scope.errorScope) {
                                            return false;
                                        }

                                        value[key] = subScope.value;

                                        break;

                                }

                                break;
                        }

                    }

                }

                // Parse an array into value.
                // Returns true if successful, false if not.
                // Sets value on success, or error on failure.
                function ParseArray()
                {
                    value = [];

                    while (true) {

                        var tokenInfo = NextTokenInfo();
                        var token = tokenInfo[0];
                        var tokenRow = tokenInfo[1];
                        var tokenColumn = tokenInfo[2];

                        switch (token) {

                            case ']':
                                return true;

                            case '}':
                            case '.':
                                error = "ParseArray: expected type or ']' instead of " + token;
                                errorRow = tokenRow;
                                errorColumn = tokenColumn;
                                return false;

                            case '{':

                                valueStack.push(value);

                                if (!ParseObject()) {
                                    return false;
                                }

                                var objectValue = value;
                                value = valueStack.pop();
                                value.push(objectValue);

                                break;

                            case '[':

                                valueStack.push(value);

                                if (!ParseArray()) {
                                    return false;
                                }

                                var arrayValue = value;
                                value = valueStack.pop();
                                value.push(arrayValue);

                                break;

                            default:

                                var typeName = token;

                                subScope = {
                                    sheetName: scope.sheetName,
                                    sheet: scope.sheet,
                                    isSingleSheet: scope.isSingleSheet,
                                    isSingleCell: true,
                                    row: scope.currentRow,
                                    column: tokenColumn,
                                    rowCount: 1,
                                    columnCount: 1,
                                    typeName: typeName,
                                    alreadyIndented: true,
                                    inTable: true,
                                    index: index,
                                    tableRow: scope.currentRow,
                                    tableColumn: firstHeaderColumn,
                                    tableRows: 1,
                                    tableColumns: headerColumns,
                                    inTableArray: true,
                                    inTableArrayIndex: subScope.value.length
                                };

                                scope.subScopes.push(subScope);

                                scope.errorScope = LoadJSONFromSheet(sheets, ranges, subScope);
                                if (scope.errorScope) {
                                    return false;
                                }

                                value.push(subScope.value);

                                break;
                        }

                    }
                }

                // Now parse this row into an array or object into value.
                if (ParseTop()) {
                    scope.value.push(value);
                } else {
                    scope.error = error;
                    scope.errorSheetName = scope.sheetName;
                    scope.errorRow = errorRow;
                    scope.errorColumn = errorColumn;
                    return scope; // error
                }

                // Go to the next row.
                scope.currentRow++;

            }

            // Return the number of rows actually used.
            scope.rowsUsed = scope.currentRow - scope.row;

            return null; // success

        default:
            scope.error = "Unexpected typeName: " + scope.typeName;
            scope.errorSheetName = scope.sheetName;
            scope.errorRow = scope.currentRow;
            scope.errorColumn = scope.currentColumn;
            return scope; // error

    }

}


////////////////////////////////////////////////////////////////////////
