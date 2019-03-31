//////////////////////////////////////////////////////////////////////
// google-spreadsheet.js
// Google Spreadsheet Stuff
// Don Hopkins, Ground Up Software.


// Client ID and API key from the Developer Console
var CLIENT_ID = '331794807650-5ft0c4b90tdgjl8npo300mjn7cbk5b60.apps.googleusercontent.com';
var API_KEY = 'AIzaSyChi1hvAB609E6OkR2X69tNRhWu8n3Y6w0';

// Array of API discovery doc URLs for APIs used by the quickstart
var DISCOVERY_DOCS = ["https://sheets.googleapis.com/$discovery/rest?version=v4"];

// Authorization scopes required by the API; multiple scopes can be
// included, separated by spaces.
var SCOPES = "https://www.googleapis.com/auth/spreadsheets.readonly";

var authorizeButton = document.getElementById('authorize-button');
var signoutButton = document.getElementById('signout-button');

/**
 *  On load, called to load the auth2 library and API client library.
 */
function handleClientLoad()
{
    gapi.load('client:auth2', initClient);
}

/**
 *  Initializes the API client library and sets up sign-in state
 *  listeners.
 */
function initClient()
{
    console.log("google-spreadsheet.js: initClient");
    gapi.client.init({
        apiKey: API_KEY,
        clientId: CLIENT_ID,
        discoveryDocs: DISCOVERY_DOCS,
        scope: SCOPES
    }).then(() => {
        // Listen for sign-in state changes.
        gapi.auth2.getAuthInstance().isSignedIn.listen(updateSigninStatus);

        // Handle the initial sign-in state.
        updateSigninStatus(gapi.auth2.getAuthInstance().isSignedIn.get());
        authorizeButton.onclick = handleAuthClick;
        signoutButton.onclick = handleSignoutClick;
    });
}

/**
 *  Called when the signed in status changes, to update the UI
 *  appropriately. After a sign-in, the API is called.
 */
function updateSigninStatus(isSignedIn)
{
    if (isSignedIn) {
        authorizeButton.style.display = 'none';
        signoutButton.style.display = 'block';
        listMajors();
    } else {
        authorizeButton.style.display = 'block';
        signoutButton.style.display = 'none';
    }
}

/**
 *  Sign in the user upon button click.
 */
function handleAuthClick(event)
{
    gapi.auth2.getAuthInstance().signIn();
}

/**
 *  Sign out the user upon button click.
 */
function handleSignoutClick(event)
{
    gapi.auth2.getAuthInstance().signOut();
}

/**
 * Append a pre element to the body containing the given message
 * as its text node. Used to display the results of the API call.
 *
 * @param {string} message Text to be placed in pre element.
 */
function appendPre(message)
{
    var pre = document.getElementById('content');
    var textContent = document.createTextNode(message + '\n');
    pre.appendChild(textContent);
}

/**
 * Print the names and majors of students in a sample spreadsheet:
 * https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
 */
function listMajors()
{
    gapi.client.sheets.spreadsheets.values.get({
        //spreadsheetId: '1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms',
        spreadsheetId: '1548PY5g24efnxO6MruaJ2iVks14QVXdN7UOHzAIPIvE',
        range: 'Class Data!A1:G'
    }).then((response) => {
        var range = response.result;
        var text = "";
        if (range.values.length > 0) {
            var headers = range.values[0];
            for (var i = 1; i < range.values.length; i++) {
                var row = range.values[i];
                var a = [];
                for (var j = 0; j < row.length; j++) {
                    a.push(headers[j] + ": " + row[j]);
                }
                text += a.join(', ') + '\n';
            }
        } else {
            text += 'No data found.';
        }
        SetOutput(text);
    }, (response) => {
        SetOutput('Error: ' + response.result.error.message);
    });
}


