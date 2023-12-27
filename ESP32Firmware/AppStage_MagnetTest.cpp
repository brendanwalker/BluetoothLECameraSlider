#include "App.h"
#include "AppStage_MagnetTest.h"
#include "HallSensorManager.h"
#include <Adafruit_SSD1306.h>

//-- statics ----
const char* AppStage_MagnetTest::APP_STAGE_NAME = "MagnetTest";
AppStage_MagnetTest* AppStage_MagnetTest::s_instance= nullptr;

AppStage_MagnetTest::AppStage_MagnetTest(App* app)
	: AppStage(app, AppStage_MagnetTest::APP_STAGE_NAME)
{ 
  s_instance= this;
}

void AppStage_MagnetTest::enter()
{
  Serial.println("Enter Magnet Test");

  AppStage::enter();
  App::getInstance()->pushInputListener(this);
}

void AppStage_MagnetTest::exit()
{
  App::getInstance()->popInputListener();
  AppStage::exit();

  Serial.println("Exit Magnet Test");
}

void AppStage_MagnetTest::onRotaryButtonClicked(class Button2* button)
{
  App::getInstance()->popAppState();
}

void AppStage_MagnetTest::render()
{
  App* app= App::getInstance();
  Adafruit_SSD1306* display= app->getDisplay();
  HallSensorManager* magnetTest= HallSensorManager::getInstance();
  bool panState= magnetTest->isPanSensorActive();
  bool tiltState= magnetTest->isTiltSensorActive();
  bool slideMinState= magnetTest->isSlideMinSensorActive();
  bool slideMaxState= magnetTest->isSlideMaxSensorActive();

  display->clearDisplay();
  display->setTextSize(1);

  int yPos= 4;
  display->setCursor(2, yPos);
  display->print("> Back");
  yPos+= 10;

  display->setCursor(12, yPos);
  display->printf("Pan: %s", panState ? "On" : "Off");
  yPos+= 10;

  display->setCursor(12, yPos);
  display->printf("Tilt: %s", tiltState ? "On" : "Off");
  yPos+= 10;

  display->setCursor(12, yPos);
  display->printf("Slide Min: %s", slideMinState ? "On" : "Off");
  yPos+= 10;

  display->setCursor(12, yPos);
  display->printf("Slide Max: %s", slideMaxState ? "On" : "Off");

  display->display();
}