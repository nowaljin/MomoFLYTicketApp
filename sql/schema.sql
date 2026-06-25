CREATE DATABASE IF NOT EXISTS airline_reservation_db;
USE airline_reservation_db;

DROP TABLE IF EXISTS tickets;
DROP TABLE IF EXISTS reservations;
DROP TABLE IF EXISTS flights;
DROP TABLE IF EXISTS customers;

CREATE TABLE customers (
    customer_id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(30) NOT NULL,
    email VARCHAR(100) NOT NULL
);

CREATE TABLE flights (
    flight_id INT AUTO_INCREMENT PRIMARY KEY,
    flight_number VARCHAR(20) NOT NULL UNIQUE,
    origin VARCHAR(60) NOT NULL,
    destination VARCHAR(60) NOT NULL,
    departure_time DATETIME NOT NULL,
    arrival_time DATETIME NOT NULL,
    seat_capacity INT NOT NULL
);

CREATE TABLE reservations (
    reservation_id INT AUTO_INCREMENT PRIMARY KEY,
    reservation_code VARCHAR(20) NOT NULL UNIQUE,
    customer_id INT NOT NULL,
    flight_id INT NOT NULL,
    reservation_status ENUM('Reserved', 'Cancelled', 'CheckedIn') NOT NULL DEFAULT 'Reserved',
    reservation_date DATETIME NOT NULL,
    CONSTRAINT fk_reservation_customer
        FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_reservation_flight
        FOREIGN KEY (flight_id) REFERENCES flights(flight_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE tickets (
    ticket_id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_number VARCHAR(20) NOT NULL UNIQUE,
    reservation_id INT NOT NULL,
    seat_number VARCHAR(10) NOT NULL,
    fare_amount DECIMAL(10, 2) NOT NULL,
    ticket_status ENUM('Issued', 'Cancelled', 'Refunded') NOT NULL DEFAULT 'Issued',
    boarding_status ENUM('NotBoarded', 'Boarded') NOT NULL DEFAULT 'NotBoarded',
    CONSTRAINT fk_ticket_reservation
        FOREIGN KEY (reservation_id) REFERENCES reservations(reservation_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
);
