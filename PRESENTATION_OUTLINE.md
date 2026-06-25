# Presentation Outline

## 1. Project Theme

Airline Ticket Reservation Management System built with C# Windows Forms and MongoDB Atlas.

## 2. Target Users

- Airline counter staff
- Travel agency staff
- System administrator

## 3. Main Goal

The system manages customers, flights, reservations, tickets, and boarding status. It separates customer ID, reservation code, and ticket number so each business item can be tracked clearly.

## 4. Database Design

Collections:

- `customers`
- `flights`
- `reservations`
- `tickets`

Relationships:

- One customer can have many reservations.
- One flight can have many reservations.
- One reservation can have a ticket.

## 5. Main Functions

- Add records
- Search records
- Update records
- Delete records
- Show reservation details with lookup logic
- Manage cancelled and boarded statuses

## 6. Lookup Report Explanation

The report combines reservation, customer, flight, and ticket collections. In MongoDB, this can be done with `$lookup`, similar to a SQL JOIN. This makes it possible to see customer name, flight number, ticket number, seat number, and current status in one list.

## 7. Demonstration Flow

1. Start the app.
2. Click `Initialize DB`.
3. Add a new customer.
4. Add or edit a reservation.
5. Open the lookup view.
6. Show reservation and boarding statuses.

## 8. Conclusion

The project meets the assignment requirements by using C# Windows Forms, MongoDB collections, CRUD operations, related-data lookup, and fictional sample data.
