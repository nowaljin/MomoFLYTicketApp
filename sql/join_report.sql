USE airline_reservation_db;

SELECT
    r.reservation_code AS ReservationCode,
    c.full_name AS CustomerName,
    f.flight_number AS FlightNumber,
    f.origin AS Origin,
    f.destination AS Destination,
    f.departure_time AS DepartureTime,
    t.ticket_number AS TicketNumber,
    t.seat_number AS SeatNumber,
    r.reservation_status AS ReservationStatus,
    t.ticket_status AS TicketStatus,
    t.boarding_status AS BoardingStatus
FROM reservations r
JOIN customers c ON r.customer_id = c.customer_id
JOIN flights f ON r.flight_id = f.flight_id
LEFT JOIN tickets t ON r.reservation_id = t.reservation_id
ORDER BY f.departure_time, r.reservation_code;
