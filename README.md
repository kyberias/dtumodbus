# Modbus to MQTT integration for Hoymiles DTU Pro

This program reads solar panel status from Hoymiles DTU Pro and sends it to a given MQTT topic. The DTU device is polled approximately once per minute using the Modbus TCP protocol.

Hoymiles DTU Pro's implementation of Modbus TCP is extremely flaky. By introducing suitable delays this programs tries to make the communication more robust.

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

# Usage

Update configuration in appsettings.json and run the executable.

