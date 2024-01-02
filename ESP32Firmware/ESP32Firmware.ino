// Adapted from https://github.com/clehn8ok/DIY3AxisCameraSlider

#include <Arduino.h>
#include <SPI.h>                            //Import libraries to control the OLED display
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <math.h>
#include "Button2.h"
#include "RotaryEncoder.h"
#include "algorithm"

#include "App.h"
#include "AppStage.h"
#include "AppStage_MainMenu.h"
#include "AppStage_Monitor.h"
#include "AppStage_MagnetTest.h"
#include "AppStage_MotorTest.h"
#include "AppStage_SliderSettings.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "HallSensorManager.h"
#include "SliderManager.h"

#define SCREEN_WIDTH 128                    // OLED display width, in pixels
#define SCREEN_HEIGHT 64                    // OLED display height, in pixels

#define OLED_RESET -1                                                           // Reset pin # (or -1 if sharing Arduino reset pin)
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);       // Declaration for an SSD1306 display connected to I2C (SDA, SCL pins)

#define ROTARY_ENCODER_A_PIN 32
#define ROTARY_ENCODER_B_PIN 13
#define ROTARY_ENCODER_BUTTON_PIN 25
#define ROTARY_ENCODER_STEPS 4

#define HALL_PAN_PIN 36
#define HALL_TILT_PIN 37
#define HALL_SLIDE_MIN_PIN 38
#define HALL_SLIDE_MAX_PIN 39

#define MOTOR_EN_PIN 33 

#define MOTOR_PAN_STEP_PIN 17
#define MOTOR_PAN_DIRECTION_PIN 16

#define MOTOR_TILT_STEP_PIN 26
#define MOTOR_TILT_DIRECTION_PIN 27

#define MOTOR_SLIDE_STEP_PIN 14
#define MOTOR_SLIDE_DIRECTION_PIN 12

#define WAKEUP_TOUCH_THRESHOLD   40

// Persistent storage
ConfigManager configManager;

// Manages the slider engine
SliderState sliderState(
  MOTOR_EN_PIN,
  MOTOR_PAN_STEP_PIN, MOTOR_PAN_DIRECTION_PIN,
  MOTOR_TILT_STEP_PIN, MOTOR_TILT_DIRECTION_PIN,
  MOTOR_SLIDE_STEP_PIN, MOTOR_SLIDE_DIRECTION_PIN);

// Rotary Encoder button
RotaryHalfStep rotaryEncoder(ROTARY_ENCODER_A_PIN, ROTARY_ENCODER_B_PIN);
Button2 rotaryButton;

// Hall Effect Sensors
HallSensorManager hallSensorManager(HALL_PAN_PIN, HALL_TILT_PIN, HALL_SLIDE_MIN_PIN, HALL_SLIDE_MAX_PIN);

// BluetoothLE Manager
BLEManager bleManager(&configManager);

// Application States
App app(&display, &rotaryEncoder);
AppStage_MainMenu mainMenu(&app);
AppStage_Monitor monitorMenu(&app);
AppStage_MagnetTest magnetTestMenu(&app);
AppStage_MotorTest motorTestMenu(&app);
AppStage_SliderCalibration sliderCalibration(&app);
AppStage_SliderSettings sliderSettings(&app);

#if defined(ARDUINO_ARCH_ESP8266) || defined(ARDUINO_ARCH_ESP32)
ICACHE_RAM_ATTR
#endif
void rotaryInterrupt()
{
    rotaryEncoder.read();
}

void rotaryValueChanged(RotaryHalfStep& rotaryEncoder)
{
  app.onRotaryEncoderValueChanged(&rotaryEncoder);
}

void rotaryButtonClicked(Button2& button)
{
  app.onRotaryButtonClicked(&button);
}

void setup()
{
  Serial.begin(115200);
  Serial.print("Begin Setup");

  // Initialize rotary encoder
  Serial.println(F("Setup Rotary Encoder"));
  rotaryEncoder.setChangedHandler(rotaryValueChanged);
  attachInterrupt(digitalPinToInterrupt(ROTARY_ENCODER_A_PIN), rotaryInterrupt, CHANGE);
  attachInterrupt(digitalPinToInterrupt(ROTARY_ENCODER_B_PIN), rotaryInterrupt, CHANGE);

  // Initialize button on rotary encoder
  rotaryButton.begin(ROTARY_ENCODER_BUTTON_PIN);
  rotaryButton.setTapHandler(rotaryButtonClicked);

  // Initialize hall effect sensors
  hallSensorManager.setup();

  // Setup display
  Serial.println(F("Setup Display"));
  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C))          //Connect to the OLED display
  {
    Serial.println(F("SSD1306 allocation failed"));       //If connection fails
    for(;;);                                              //Don't proceed, loop forever
  }
  display.clearDisplay();                                 //Clear the display
  display.setTextColor(SSD1306_WHITE);                    //Set the text colour to white

  // Fetch stored config settings
  Serial.println(F("Setup Config Manager"));
  configManager.load();

  // Setup the Stepper Motor Manager
  Serial.println(F("Setup Stepper Motor Manager"));
  sliderState.setup();

  // Manage BluetoothLE servive + Events
  Serial.println(F("Setup Bluetooth Manager"));
  bleManager.setup();

  // Initialize application state machine
  Serial.println(F("Setup App State Machine"));
  app.setup();

  // Push Main Menu on to the stack first
  app.pushAppStage(&mainMenu);

  // Then push on slider calibration state, if no saved calibration exists
  if (!sliderState.areSteppersCalibrated())
  {
    sliderCalibration.setAutoExitOnComplete(true);
    app.pushAppStage(&sliderCalibration);  
  }
}

void loop() 
{
  // Process Input events from the Rotary Encoder
  rotaryEncoder.loop();
  rotaryButton.loop();

  // Update motor positions stored in config
  sliderState.loop();

  // Update hall effect sensor state
  hallSensorManager.loop();

  // Process bluetooth events
  bleManager.loop();
  
  // Update UI
  app.loop();
}