# Club

Shared language for facility bookings and their management.

## Language

**Booking owner**:
The customer to whom a booking belongs, including when an admin creates it on their behalf.
_Avoid_: Creating admin (when referring to ownership)

**Guest booking**:
A booking created by an acting admin for a booking owner without a customer account. The booking stores the guest's name and optional contact details, has no user relationship, and uses public rates. Admins manage the booking and receive payment in the club; guests have no authenticated self-service access.

**Acting admin**:
The administrator performing a booking action on behalf of a customer, distinct from the booking owner.
