-- PostgreSQL schema for Theater Ticketing System
-- Suggested database name: theater_ticketing

-- CREATE DATABASE theater_ticketing;
-- \c theater_ticketing;

CREATE TABLE IF NOT EXISTS performances (
    id              SERIAL PRIMARY KEY,
    play_name       VARCHAR(200) NOT NULL,
    start_time      TIMESTAMP NOT NULL,
    duration_minutes INTEGER NOT NULL CHECK (duration_minutes > 0),
    ticket_price    NUMERIC(12, 2) NOT NULL CHECK (ticket_price > 0),
    status          VARCHAR(20) NOT NULL DEFAULT 'NOT_STARTED'
                        CHECK (status IN ('NOT_STARTED', 'IN_PROGRESS', 'ENDED')),
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_performances_play_name ON performances (play_name);
CREATE INDEX IF NOT EXISTS idx_performances_start_time ON performances (start_time);
CREATE INDEX IF NOT EXISTS idx_performances_status ON performances (status);

CREATE TABLE IF NOT EXISTS bookings (
    id              SERIAL PRIMARY KEY,
    performance_id  INTEGER NOT NULL REFERENCES performances(id) ON DELETE RESTRICT,
    customer_name   VARCHAR(150) NOT NULL,
    seat_type       VARCHAR(20) NOT NULL CHECK (seat_type = 'REGULAR'),
    ticket_qty      INTEGER NOT NULL CHECK (ticket_qty > 0),
    unit_price      NUMERIC(12, 2) NOT NULL CHECK (unit_price > 0),
    total_amount    NUMERIC(12, 2) NOT NULL CHECK (total_amount > 0),
    created_at      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_bookings_performance_id ON bookings (performance_id);
CREATE INDEX IF NOT EXISTS idx_bookings_customer_name ON bookings (customer_name);

CREATE TABLE IF NOT EXISTS seat_assignments (
    id              SERIAL PRIMARY KEY,
    booking_id      INTEGER NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    performance_id  INTEGER NOT NULL REFERENCES performances(id) ON DELETE CASCADE,
    seat_row        CHAR(1) NOT NULL CHECK (seat_row BETWEEN 'A' AND 'J'),
    seat_number     INTEGER NOT NULL CHECK (seat_number BETWEEN 1 AND 10),
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_performance_seat UNIQUE (performance_id, seat_row, seat_number),
    CONSTRAINT uq_booking_seat UNIQUE (booking_id, seat_row, seat_number)
);

CREATE INDEX IF NOT EXISTS idx_seat_assignments_booking_id ON seat_assignments (booking_id);
CREATE INDEX IF NOT EXISTS idx_seat_assignments_performance_id ON seat_assignments (performance_id);

-- Ensure seat_assignments.performance_id matches booking.performance_id.
CREATE OR REPLACE FUNCTION fn_validate_seat_assignment_performance()
RETURNS TRIGGER AS $$
DECLARE
    booking_performance_id INTEGER;
BEGIN
    SELECT performance_id INTO booking_performance_id
    FROM bookings
    WHERE id = NEW.booking_id;

    IF booking_performance_id IS NULL THEN
        RAISE EXCEPTION 'Booking % does not exist', NEW.booking_id;
    END IF;

    IF NEW.performance_id <> booking_performance_id THEN
        RAISE EXCEPTION 'performance_id mismatch for booking %', NEW.booking_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_validate_seat_assignment_performance ON seat_assignments;
CREATE TRIGGER trg_validate_seat_assignment_performance
BEFORE INSERT OR UPDATE ON seat_assignments
FOR EACH ROW
EXECUTE FUNCTION fn_validate_seat_assignment_performance();

-- Bonus reporting view: seat count by type per performance.
CREATE OR REPLACE VIEW v_performance_seat_type_summary AS
SELECT
    p.id AS performance_id,
    p.play_name,
    p.start_time,
    b.seat_type,
    SUM(b.ticket_qty)::INTEGER AS total_tickets
FROM performances p
JOIN bookings b ON b.performance_id = p.id
GROUP BY p.id, p.play_name, p.start_time, b.seat_type
ORDER BY p.start_time, p.play_name, b.seat_type;
