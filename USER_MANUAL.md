# User Manual

## Purpose

This application helps airline counter staff, travel agency staff, or an administrator manage ticket reservations using a C# Windows Forms screen and a MongoDB Atlas database.

## First Setup

1. Open the project in VS Code.
2. Run `dotnet restore` and `dotnet run`.
3. Confirm that the MongoDB URI is shown at the top of the app.
4. Click `Initialize DB` to create the MongoDB collections and insert fictional data.

## CRUD Management

1. Select a collection: `customers`, `flights`, `reservations`, or `tickets`.
2. Enter values in the form fields.
3. Click `Add` to register a new record.
4. Select a row from the table, edit the form fields, then click `Update`.
5. Select a row and click `Delete` to remove it.
6. Type in the search box to search the selected collection.

## Reservation Lookup View

Open the `Reservation Lookup View` tab and click `Refresh Lookup View`. This shows customer name, flight number, ticket number, seat number, reservation status, ticket status, and boarding status together.

## Status Examples

- Reservation status: `Reserved`, `Cancelled`, `CheckedIn`.
- Ticket status: `Issued`, `Cancelled`, `Refunded`.
- Boarding status: `NotBoarded`, `Boarded`.
