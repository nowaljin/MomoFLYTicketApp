use("airline_reservation_db");

db.customers.drop();
db.flights.drop();
db.reservations.drop();
db.tickets.drop();

db.customers.insertMany([
  { customer_id: 1, full_name: "Aylin Demir", phone: "090-1111-2222", email: "aylin.demir@example.com" },
  { customer_id: 2, full_name: "Kenji Sato", phone: "090-3333-4444", email: "kenji.sato@example.com" },
  { customer_id: 3, full_name: "Mina Carter", phone: "090-5555-6666", email: "mina.carter@example.com" }
]);

db.flights.insertMany([
  { flight_id: 1, flight_number: "BB101", origin: "Tokyo", destination: "Osaka", departure_time: ISODate("2026-07-01T09:00:00Z"), arrival_time: ISODate("2026-07-01T10:20:00Z"), seat_capacity: 180 },
  { flight_id: 2, flight_number: "BB205", origin: "Osaka", destination: "Fukuoka", departure_time: ISODate("2026-07-02T13:30:00Z"), arrival_time: ISODate("2026-07-02T14:45:00Z"), seat_capacity: 160 },
  { flight_id: 3, flight_number: "BB330", origin: "Tokyo", destination: "Sapporo", departure_time: ISODate("2026-07-03T18:15:00Z"), arrival_time: ISODate("2026-07-03T19:50:00Z"), seat_capacity: 200 }
]);

db.reservations.insertMany([
  { reservation_id: 1, reservation_code: "RSV-1001", customer_id: 1, flight_id: 1, reservation_status: "Reserved", reservation_date: ISODate("2026-06-20T10:10:00Z") },
  { reservation_id: 2, reservation_code: "RSV-1002", customer_id: 2, flight_id: 2, reservation_status: "CheckedIn", reservation_date: ISODate("2026-06-21T11:25:00Z") },
  { reservation_id: 3, reservation_code: "RSV-1003", customer_id: 3, flight_id: 3, reservation_status: "Cancelled", reservation_date: ISODate("2026-06-22T15:40:00Z") }
]);

db.tickets.insertMany([
  { ticket_id: 1, ticket_number: "TKT-9001", reservation_id: 1, seat_number: "12A", fare_amount: NumberDecimal("14500.00"), ticket_status: "Issued", boarding_status: "NotBoarded" },
  { ticket_id: 2, ticket_number: "TKT-9002", reservation_id: 2, seat_number: "08C", fare_amount: NumberDecimal("12800.00"), ticket_status: "Issued", boarding_status: "Boarded" },
  { ticket_id: 3, ticket_number: "TKT-9003", reservation_id: 3, seat_number: "21F", fare_amount: NumberDecimal("17000.00"), ticket_status: "Cancelled", boarding_status: "NotBoarded" }
]);
