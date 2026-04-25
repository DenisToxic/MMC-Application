#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <WiFiClient.h>
#include "secrets.h"

#ifndef STATION_ID
#define STATION_ID "TEST-CELL-01"
#endif

#ifndef DEVICE_ID
#define DEVICE_ID "ESP-001"
#endif

#ifndef SERVER_URL
#define SERVER_URL "http://192.168.188.69:5000/api/telemetry"
#endif

const char* ssid = WIFI_SSID;
const char* password = WIFI_PASSWORD;
const char* serverUrl = SERVER_URL;
const char* stationId = STATION_ID;
const char* deviceId = DEVICE_ID;

unsigned long lastSendTime = 0;
const unsigned long interval = 5000; // 5 seconds
unsigned long cycleCount = 0;


void connectWiFi() {
  WiFi.begin(ssid, password);

  Serial.print("Connecting to WiFi");

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nConnected!");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());
}


float generateTemperature() {
  return random(200, 350) / 10.0; // 20.0 - 35.0 C
}

float generateVibration() {
  return random(0, 100) / 10.0; // 0.0 - 10.0
}

int generateLoad() {
  return random(10, 95); // 10% - 95%
}

String generateTestResult() {
  int value = random(0, 100);

  if (value < 10) {
    return "Fail";
  }

  if (value < 35) {
    return "Running";
  }

  return "Pass";
}

void sendTelemetry() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi lost. Reconnecting...");
    connectWiFi();
    return;
  }

  WiFiClient client;
  HTTPClient http;

  http.begin(client, serverUrl);
  http.setTimeout(5000);
  http.addHeader("Content-Type", "application/json");

  cycleCount++;

  String payload = "{";
  payload += "\"deviceId\":\"" + String(deviceId) + "\",";
  payload += "\"stationId\":\"" + String(stationId) + "\",";
  payload += "\"cycleCount\":" + String(cycleCount) + ",";
  payload += "\"uptimeMs\":" + String(millis()) + ",";
  payload += "\"temperatureC\":" + String(generateTemperature()) + ",";
  payload += "\"vibrationMmS\":" + String(generateVibration()) + ",";
  payload += "\"loadPercent\":" + String(generateLoad()) + ",";
  payload += "\"testResult\":\"" + generateTestResult() + "\",";
  payload += "\"alarmCode\":null,";
  payload += "\"alarmText\":null";
  payload += "}";

  Serial.println("Sending:");
  Serial.println(serverUrl);
  Serial.println(payload);

  int httpResponseCode = http.POST(payload);

  if (httpResponseCode > 0) {
    Serial.print("Response code: ");
    Serial.println(httpResponseCode);

    String response = http.getString();
    Serial.println("Response:");
    Serial.println(response);
  } else {
    Serial.print("Error sending POST: ");
    Serial.println(httpResponseCode);
  }

  http.end();
}


void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("\nESP8266 Industrial Sensor Node Starting...");

  connectWiFi();

  randomSeed(analogRead(A0));
}


void loop() {
  unsigned long now = millis();

  if (now - lastSendTime >= interval) {
    lastSendTime = now;
    sendTelemetry();
  }
}
