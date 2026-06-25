# Airline Ticket Reservation Management System

This is a C# Windows Forms application for an airline ticket reservation assignment. It uses MongoDB Atlas collections to manage customers, flights, reservations, tickets, and boarding status.

## English Assignment Explanation

The goal is to build a small airline ticket reservation management system. Each customer, reservation, and ticket must have its own unique ID or number. The application should allow staff members to register, search, update, and delete data from a Windows Forms screen. The database uses multiple related MongoDB collections, and the application displays combined information such as customer name, flight number, ticket number, and reservation status. In MongoDB, this relationship view can be produced with `$lookup`, which works like a join-style report.

## What the System Does

- Stores customers, flights, reservations, and tickets in separate MongoDB collections.
- Uses customer IDs, reservation codes, and ticket numbers.
- Supports CRUD operations: add, search, update, and delete.
- Shows a combined reservation report using related collection lookup logic.
- Manages reservation status, ticket status, and boarding status.
- Uses only fictional sample data.

## Main Collections

- `customers`: customer information.
- `flights`: flight number, route, time, and seat capacity.
- `reservations`: connects a customer to a flight.
- `tickets`: ticket number, seat, fare, and boarding status.

## How to Run in VS Code

1. Open the `AirlineTicketReservationSystem` folder in VS Code.
2. Run these commands in the VS Code terminal:

```powershell
dotnet restore
dotnet run
```

3. The MongoDB URI is already filled into the connection box.
4. Click `Initialize DB` to create/reset the `airline_reservation_db` database collections and insert fictional sample data.
5. Use the `CRUD Management` tab to add, search, update, and delete records.
6. Use the `Reservation Lookup View` tab to display customer, flight, ticket, and status information together.

## Submission Files

- C# project: this folder.
- MongoDB files: `mongodb/seed.js`, `mongodb/lookup_report.js`.
- Screen captures: take screenshots after running the app.
- Operation guide: `USER_MANUAL.md`.
- Presentation outline: `PRESENTATION_OUTLINE.md`.
