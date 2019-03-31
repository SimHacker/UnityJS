----

Install the "clasp" utility for managing google App Scripts.

https://developers.google.com/apps-script/guides/clasp

npm install @google/clasp -g

Log in:

clasp login

https://codelabs.developers.google.com/codelabs/clasp/#0

----

JSONster.gs is an app script bound to a spreadsheet.
The spreadsheet is the "container" of the script.
The script is bound to the spreadsheet, so it has permission and access.
The script is also contained in a project associated with the sheet.
For each instance of a spreadsheet with a script bound to it, there is a project.

Container-bound scripts:

https://developers.google.com/apps-script/guides/bound

Extending Google Sheets:
https://developers.google.com/apps-script/guides/sheets

----

Spreadsheet:
JSONster World

Spreadsheet ID:
1nh8tlnanRaTmY8amABggxc0emaXCukCYR18EGddiC4w

https://docs.google.com/spreadsheets/d/1nh8tlnanRaTmY8amABggxc0emaXCukCYR18EGddiC4w/edit#gid=0

Project:
JSONster - project-id-0993534538869130315

https://console.cloud.google.com/home/dashboard?project=project-id-0993534538869130315

https://script.google.com/macros/d/MdVIZmXeb4OwWm2sBIoxhEaJGBFN6q3R9/edit?uiv=2&mid=ACjPJvFXc9jkX6DAFcd80AyFaLCb9e5r5ZMFA8rL1jI_Z9-StpMI2Q3msLV_tNibUcOanOKPLX9XYUbqTIXXb-QMHqf7L0yzMIbHSx9vo75u2wZQ-nWl9AHVGgA479Lpz0-z0lDXv91wbqwo

Script:
JSONster.gs

Script ID: (project properties)
19SOWT78niSnnYTM7eCAk7kMm2h0j4E1I2ePmeqfSiR5HFF69_zn8r5dR

----

Spreadsheet:
JSONster World Private

Spreadsheet ID:
1m7FANuMTPAfugSTw20pHSSpTF-Ugj0Hg11Z52nRtuLY

https://docs.google.com/spreadsheets/d/1m7FANuMTPAfugSTw20pHSSpTF-Ugj0Hg11Z52nRtuLY/edit#gid=0

Project:
JSONster - project-id-0997531898314544896

https://console.cloud.google.com/home/dashboard?project=project-id-0997531898314544896

https://script.google.com/macros/d/McSdmu8v3rRodOCN_tSnXxa3U34q5xDmi/edit?uiv=2&mid=ACjPJvGlrHTuqHGkeEYj8et3f4VvG9mINJ9Ov27A4E0-Jlt0P_IcS2PcByZOHoRuxzd6DDPNK9t87sJyPah1rDr4Gop7Q5djYGUyP8RSMJ61L-0fH2cR00E2SJJVxdN4ZYR0dk-HOVpvkts8

Script:
JSONster.gs

Script ID: (project properties)
19W40zuYAaI6CXrOY-1qpFm3zRl-WuKo5XQOFuwbMqdZE3B_tq4-dxz52

----

Clone the script:

cd JSONster

clasp clone 19SOWT78niSnnYTM7eCAk7kMm2h0j4E1I2ePmeqfSiR5HFF69_zn8r5dR

clasp pull

rm -f "*~" ; clasp push

NOTE: In order for clasp push to work, you have to go to the
usersettings of the script project and enable the Google Apps Script
API. https://script.google.com/home/usersettings

NOTE: In order for clasp push to work, there may not be any extraneous
files in the directory, including backups like "JSONster.js~" or
"README.txt".

NOTE: In the project, the script is named "sheet.gs", but in the
directory, it's named "sheet.js". While in Unity's StreamingAssets
directory, it's named "sheet.jss". Go figure.

NOTE: clasp push will fail if there is a syntax error in the
JavaScript file.

----
