use("airline_reservation_db");

db.reservations.aggregate([
  { $lookup: { from: "customers", localField: "customer_id", foreignField: "customer_id", as: "customer" } },
  { $lookup: { from: "flights", localField: "flight_id", foreignField: "flight_id", as: "flight" } },
  { $lookup: { from: "tickets", localField: "reservation_id", foreignField: "reservation_id", as: "ticket" } },
  { $unwind: "$customer" },
  { $unwind: "$flight" },
  { $unwind: { path: "$ticket", preserveNullAndEmptyArrays: true } },
  {
    $project: {
      _id: 0,
      ReservationCode: "$reservation_code",
      CustomerName: "$customer.full_name",
      FlightNumber: "$flight.flight_number",
      Origin: "$flight.origin",
      Destination: "$flight.destination",
      DepartureTime: "$flight.departure_time",
      TicketNumber: "$ticket.ticket_number",
      SeatNumber: "$ticket.seat_number",
      ReservationStatus: "$reservation_status",
      TicketStatus: "$ticket.ticket_status",
      BoardingStatus: "$ticket.boarding_status"
    }
  }
]);
