USE airline_reservation_db;

INSERT INTO customers (full_name, phone, email) VALUES
('Aylin Demir', '090-1111-2222', 'aylin.demir@example.com'),
('Kenji Sato', '090-3333-4444', 'kenji.sato@example.com'),
('Mina Carter', '090-5555-6666', 'mina.carter@example.com');

INSERT INTO flights (flight_number, origin, destination, departure_time, arrival_time, seat_capacity) VALUES
('BB101', 'Tokyo', 'Osaka', '2026-07-01 09:00:00', '2026-07-01 10:20:00', 180),
('BB205', 'Osaka', 'Fukuoka', '2026-07-02 13:30:00', '2026-07-02 14:45:00', 160),
('BB330', 'Tokyo', 'Sapporo', '2026-07-03 18:15:00', '2026-07-03 19:50:00', 200);

INSERT INTO reservations (reservation_code, customer_id, flight_id, reservation_status, reservation_date) VALUES
('RSV-1001', 1, 1, 'Reserved', '2026-06-20 10:10:00'),
('RSV-1002', 2, 2, 'CheckedIn', '2026-06-21 11:25:00'),
('RSV-1003', 3, 3, 'Cancelled', '2026-06-22 15:40:00');

INSERT INTO tickets (ticket_number, reservation_id, seat_number, fare_amount, ticket_status, boarding_status) VALUES
('TKT-9001', 1, '12A', 14500.00, 'Issued', 'NotBoarded'),
('TKT-9002', 2, '08C', 12800.00, 'Issued', 'Boarded'),
('TKT-9003', 3, '21F', 17000.00, 'Cancelled', 'NotBoarded');
