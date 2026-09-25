# RideMatchingSystem
The system is designed similar to a real-world ride-hailing application where:

Drivers can register and go Online/Offline
Driver GPS location is continuously updated
Riders can dynamically select Pickup and Drop locations
The backend finds nearby available drivers
Drivers can Accept or Reject ride requests
Rejected drivers are excluded for that specific ride
The ride follows a complete lifecycle
Drivers are automatically released after ride completion/cancellation

1. Create a Driver

Open the Driver screen:

/driver.html

Register a driver with sample data:

Name: Raj
Phone: 9999999999
Vehicle: GJ01AB1234

After registration, select Raj.

Allow the browser to access GPS location.

Click:

Go Online

The driver status should become:

Available

Expected result:

Raj
Status: Available
Location: Current GPS Location
2. Verify Driver GPS Tracking

Once the driver is Online, the browser continuously sends the driver's current GPS coordinates to the backend.

API:

POST /api/drivers/{driverId}/location

Example data:

{
  "latitude": 23.0225,
  "longitude": 72.5714
}

The backend stores/updates the driver's latest location.

The purpose of this functionality is to ensure that ride matching uses the driver's latest available location, rather than a hardcoded location.

3. Create Additional Drivers

For proper testing of the matching algorithm, create multiple drivers.

Example:

Driver A
Name: Raj
Phone: 9999999999
Vehicle: GJ01AB1234

Driver B
Name: Amit
Phone: 9999999998
Vehicle: GJ01AB5678

Driver C
Name: Rahul
Phone: 9999999997
Vehicle: GJ01AB9999

For each driver:

Select Driver
      ↓
Allow GPS
      ↓
Go Online
      ↓
Status = Available

Keep the drivers at different GPS locations if possible.

This allows the interviewer to verify that the system selects drivers based on actual distance.

4. Create a Rider

Open another browser tab:

/rider.html

Create/select a rider:

Name: Soham
Phone: 8888888888

The rider is now ready to book a ride.

5. Select Pickup Location

Click:

Use Current Location

OR search for a location using the location search functionality.

Example:

Ahmedabad Railway Station

Select the required location from the search results.

The application should obtain:

Pickup Address
Pickup Latitude
Pickup Longitude

The pickup location should not be hardcoded.

6. Select Drop Location

Search for the destination.

Example:

Gandhinagar

Select the required location.

The application should obtain:

Drop Address
Drop Latitude
Drop Longitude

The rider now has:

Pickup
   ↓
Drop
7. Book the Ride

Click:

Book Ride

The backend creates a new ride.

Initial ride status:

Searching

Example:

Ride ID: 101
Status: Searching
Pickup: Ahmedabad Railway Station
Drop: Gandhinagar
8. Ride Matching

The background matching process searches for available drivers.

The flow is:

Searching Ride
       ↓
Find Available Drivers
       ↓
Get Latest Driver GPS
       ↓
Calculate Distance
       ↓
Filter Eligible Drivers
       ↓
Select Nearest Driver
       ↓
Create Ride Offer

The selected driver changes from:

Available

to:

Offered

The driver should receive a ride request such as:

RIDE REQUEST

Pickup: Ahmedabad Railway Station
Drop: Gandhinagar

Distance: X KM

[Accept] [Reject]
9. Test Driver Rejection

To test the rejection scenario, let Driver A reject the ride.

Example:

Driver A
   ↓
Reject

Driver A should become:

Available

The ride should remain:

Searching

The system should record that Driver A rejected this particular ride.

Example table:

RideDriverRejections

The matching service must not offer the same ride again to Driver A.

Flow:

Ride
  ↓
Driver A
  ↓
Reject
  ↓
Save Rejection
  ↓
Exclude Driver A
  ↓
Search Next Driver
  ↓
Driver B
  ↓
Offer Ride

This prevents the same driver from repeatedly receiving a ride they already rejected.

10. Test Driver Acceptance

When Driver B accepts:

Accept Ride

The system should update:

Driver B = Busy

Ride = DriverAssigned

Expected flow:

Searching
    ↓
DriverAssigned

The rider should now see the assigned driver information.

11. Driver Arrives

The driver reaches the pickup location and clicks:

Driver Arrived

Ride status:

DriverArrived

Flow:

DriverAssigned
      ↓
DriverArrived

The driver should remain:

Busy

because the ride has not finished yet.

12. Start Ride

After the rider enters the vehicle, the driver clicks:

Start Ride

Ride status becomes:

InProgress

Flow:

DriverArrived
      ↓
InProgress

Driver status remains:

Busy
13. Complete Ride

After reaching the destination, click:

Complete Ride

Ride status becomes:

Completed

The driver should automatically be released:

Busy
  ↓
Available

Complete flow:

Searching
    ↓
DriverAssigned
    ↓
DriverArrived
    ↓
InProgress
    ↓
Completed
    ↓
Driver = Available

The same driver can now immediately receive another ride.

14. Test Rider Cancellation

Create another ride and test cancellation.

For example:

Searching
    ↓
Cancel Ride

The final status should be:

Cancelled

If a driver was already assigned:

DriverAssigned
      ↓
Cancel Ride
      ↓
Ride = Cancelled
      ↓
Driver = Available

The driver must not remain stuck in:

Busy

after the ride is cancelled.

15. Test Cancellation Before Driver Assignment

Create a new ride:

Ride = Searching

Immediately click:

Cancel Ride

Expected result:

Searching
    ↓
Cancelled

No driver should be assigned.

16. Test Cancellation After Driver Assignment

Create another ride and allow a driver to accept it.

Expected state:

Ride = DriverAssigned
Driver = Busy

Now cancel the ride from the rider screen.

Expected result:

Ride = Cancelled
Driver = Available

This verifies that the backend correctly releases the driver.

17. Test Driver Going Offline

From the driver screen, click:

Go Offline

Driver status should become:

Offline

An offline driver must not be selected for new rides.

Expected matching rule:

Offline Driver
      ↓
Not Eligible

Only drivers with the appropriate available/online state should be considered for new ride offers.

18. Test Dynamic GPS

Move/change the driver's browser GPS location or use browser location simulation.

The system should update:

Latitude
Longitude
LastUpdated

through:

POST /api/drivers/{driverId}/location

The next ride matching operation should use the driver's latest location.

This demonstrates that the system is location-based and dynamic, rather than using fixed Ahmedabad/Gandhinagar coordinates.

19. Test Multiple Drivers

Create at least three online drivers:

Driver A → 2 KM from pickup
Driver B → 5 KM from pickup
Driver C → 8 KM from pickup

Book a ride.

The matching service should evaluate the available drivers based on their latest GPS coordinates and distance from the pickup.

Then test:

Driver A → Reject

The system should exclude Driver A for that ride and continue matching.

Driver B → Offer

If Driver B accepts:

Driver B = Busy
Ride = DriverAssigned
20. Complete End-to-End Test

The interviewer can test the entire system using this flow:

CREATE DRIVER
      ↓
GO ONLINE
      ↓
ALLOW GPS
      ↓
DRIVER LOCATION UPDATED
      ↓
CREATE RIDER
      ↓
SELECT PICKUP
      ↓
SELECT DROP
      ↓
BOOK RIDE
      ↓
RIDE = SEARCHING
      ↓
MATCH AVAILABLE DRIVERS
      ↓
CALCULATE DISTANCE
      ↓
OFFER TO NEAREST DRIVER
      ↓
      ├── REJECT
      │     ↓
      │  EXCLUDE DRIVER
      │     ↓
      │  SEARCH NEXT DRIVER
      │
      └── ACCEPT
            ↓
       DRIVER = BUSY
            ↓
       DRIVER ASSIGNED
            ↓
       DRIVER ARRIVED
            ↓
       START RIDE
            ↓
       IN PROGRESS
            ↓
       COMPLETE RIDE
            ↓
       RIDE = COMPLETED
            ↓
       DRIVER = AVAILABLE
21. Complete Cancellation Test
Book Ride
    ↓
Searching
    ↓
Driver Assigned
    ↓
Driver = Busy
    ↓
Rider Cancels
    ↓
Ride = Cancelled
    ↓
Driver = Available

Also test:

Searching
    ↓
Rider Cancels
    ↓
Cancelled
22. What the Interviewer Can Verify

The following functionality can be tested directly from the application:

Driver Management
Driver registration
Driver selection
Go Online
Go Offline
Available/Busy status
Current GPS location
GPS Tracking
Browser GPS permission
Current latitude/longitude
Continuous location updates
Latest driver location used for matching
Rider Management
Rider registration
Rider selection
Dynamic pickup
Dynamic drop
Current-location selection
Location search
Ride Matching
Available-driver filtering
Distance calculation
Nearest-driver matching
Driver offer
Driver rejection
Next-driver matching
Rejection history
Ride Lifecycle
Searching
    ↓
DriverAssigned
    ↓
DriverArrived
    ↓
InProgress
    ↓
Completed
Cancellation
Searching → Cancelled

or:

DriverAssigned → Cancelled

with the driver correctly released.

Driver Reuse

After:

Completed

or an applicable cancellation:

Driver = Available

The driver can receive another ride.

23. Dynamic Location Architecture

The application does not depend on hardcoded locations such as:

Ahmedabad
Gandhinagar

The actual architecture is:

Browser GPS
     ↓
Driver Location API
     ↓
Latest Driver Coordinates
     ↓
Database
     ↓
Available Drivers
     ↓
Distance Calculation
     ↓
Ride Matching
     ↓
Ride Offer
     ↓
Driver Accept / Reject

For riders:

Current GPS / Location Search
            ↓
     Pickup Coordinates
            +
      Drop Coordinates
            ↓
         Book Ride
            ↓
      Ride Matching

This allows the same system to work with locations across India rather than being restricted to a particular city.
