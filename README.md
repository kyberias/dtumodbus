![Build and test](https://github.com/kyberias/dtumodbus/actions/workflows/buildandtest.yml/badge.svg)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE)

# Modbus to MQTT integration for Hoymiles DTU Pro

This program reads solar panel status from Hoymiles DTU Pro and sends it to given MQTT topics. The DTU device is polled approximately once per minute using the Modbus TCP protocol.

The following data is sent:

* The sum of current power (kW) of all the configured panels
* The sum of cumulative daily energy (kWh) of all the configured panels


# Configuration

The following settings are available in appsettings.json.

|Setting||
|---|---|
|mqtt:broker|Host name of the MQTT broker. The default port will be used.
|mqtt:username|Username used to login to the MQTT broker.
|mqtt:password|Password used to login to the MQTT broker.
|mqtt:totalPowerTopic|Path to the topic used to publish the current total power of the panels (kW)
|mqtt:totalTodayProductionTopic|Path to the topic used to publish the accumulated daily solar energy (kWh)
|dtu:hostname|Hostname or IP address of the DTU device.
|dtu:port|Modbus TCP Port of the DTU device.
|panels:numPanels|Number of panels to query.
|Logging|Standard .NET logging settings.

# Build

    dotnet build

# Usage

Make sure you have at least V00.02.15 of DTU firmware since earlier versions have very unstable Modbus TCP implementation.

Update configuration in appsettings.json and run the executable.

    dotnet run
