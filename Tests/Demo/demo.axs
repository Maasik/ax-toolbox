﻿set DateTime = 2010/10/28,PM
set map=demo.png
//set map=blank
set Datum=European 1950
set UTMZone=31T
set QNH = 1012
set TASKORDER=free

//task 4
TASK Task4 = XDD()
	POINT PWaypoint=SUTM (31T,300000,4603000,180m) waypoint(orange)
	POINT PMarker=SUTM (31T,300500,4603000,180m) Marker(green)
	POINT PCrosshair=SUTM (31T,300000,4603500,180m) crosshairs(red)
	POINT PTarget=SUTM (31T,300500,4603500,180m) target(100m, yellow)


	POINT T4AreaCenter = SUTM (31T,302000,4605000,180m) none()
	AREA T4Area = circle(T4AreaCenter,1000m) Default(green)
	//AREA T4AnotherArea = poly(task4area.trk) Default(green)

	//filter T4scoringPeriod = BEFORETIME(10:00:00)
	//filter T4scoringArea = Inside(T4Area);

	//POINT T4A = tafi(T4Area) marker(green)
	//POINT T4B = tafo(T4Area) marker(red)

	//RESULT t4result = D2D(T4A,T4B)