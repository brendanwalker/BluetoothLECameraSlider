#define __ASSERT_USE_STDERR

#include <assert.h>
#include <Arduino.h>
#include "App.h"
#include "AppStage.h"
#include "ConfigManager.h"
#include "RotaryEncoder.h"
#include "SliderManager.h"

App* App::s_instance= nullptr;

App::App(
  class Adafruit_SSD1306* display,
  class RotaryHalfStep* rotaryEncoder)
  : m_display(display)
  , m_rotaryEncoder(rotaryEncoder)
{
}

void App::setup()
{
  App::s_instance= this;
}

void App::loop(float deltaSeconds)
{
  autoSaveTimer-= deltaSeconds;
  if (autoSaveTimer <= 0.f)
  {
    ConfigManager::getInstance()->saveMotorPositionConfig();
    autoSaveTimer= AUTO_SAVE_DURATION;
  }

  AppStage* appStage= getCurrentAppStage();
  if (appStage != nullptr)
  {
    // Updating the app stage could cause changes to the app stack (push or pop)
    appStage->update(deltaSeconds);
  }

  appStage= getCurrentAppStage();
  if (appStage != nullptr)
  {
    appStage->render();
  }  
}

void App::pushAppStage(AppStage *appStage)
{
    assert(getAppStageStackSize() < MAX_APP_STAGE_DEPTH);

    AppStage *parentAppStage = getCurrentAppStage();
    if (parentAppStage != nullptr)
        parentAppStage->pause();

    appStage->enter();

    m_appStageStackIndex++;
    m_appStageStack[m_appStageStackIndex] = appStage;
}

void App::popAppState()
{
    AppStage *appStage = getCurrentAppStage();
    if (appStage != nullptr)
    {
        appStage->exit();

        m_appStageStackIndex--;
        if (m_appStageStackIndex >= 0)
            m_appStageStack[m_appStageStackIndex]->resume();
    }
}

// Input Events
void App::onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder)
{  
  if (m_inputStackIndex >= 0 && m_inputStackIndex < MAX_INPUT_LISTENER_DEPTH)
  {
    Serial.printf("onRotaryEncoderValueChanged - send to listener %d\n", m_inputStackIndex);
    m_inputListenerStack[m_inputStackIndex]->onRotaryEncoderValueChanged(rotaryEncoder);
  }
}

void App::onRotaryButtonClicked(class Button2* button)
{
  if (m_inputStackIndex >= 0 && m_inputStackIndex < MAX_INPUT_LISTENER_DEPTH)
  {
    Serial.printf("onButtonClicked - send to listener %d\n", m_inputStackIndex);
    m_inputListenerStack[m_inputStackIndex]->onRotaryButtonClicked(button);
  }
}

void App::pushInputListener(InputEventListener *inputListener)
{
  if (m_inputStackIndex < MAX_INPUT_LISTENER_DEPTH)
  {
    Serial.printf("Push input listener: %d->%d\n", m_inputStackIndex, m_inputStackIndex+1);
    m_inputStackIndex++;
    m_inputListenerStack[m_inputStackIndex]= inputListener;
    applyInputListenerSettings();
  }
  else
  {
    Serial.printf("ERROR: Input listener stack push failure!");
  }
}

void App::popInputListener()
{
  if (m_inputStackIndex > 0)
  {
    Serial.printf("Pop input listener: %d->%d\n", m_inputStackIndex, m_inputStackIndex-1);
    m_inputStackIndex--;
    applyInputListenerSettings();
  }
  else
  {
    Serial.printf("ERROR: Input listener stack pop failure!");
  }
}

void App::applyInputListenerSettings()
{
  if (m_inputStackIndex >= 0 && m_inputStackIndex < MAX_INPUT_LISTENER_DEPTH)
  {
    InputEventListener* inputListener= m_inputListenerStack[m_inputStackIndex];

    m_rotaryEncoder->setIsWrapped(inputListener->getIsRotaryEncoderWrapped());
    m_rotaryEncoder->setLowerBound(inputListener->getRotaryEncoderLowerBound());
    m_rotaryEncoder->setUpperBound(inputListener->getRotaryEncoderUpperBound());
    m_rotaryEncoder->resetPosition(inputListener->getRotaryEncoderDefaultValue());
  }
}

void App::save()
{
  SliderState::getInstance()->writePositionsToConfig();
  ConfigManager::getInstance()->saveMotorPositionConfig();

  m_display->clearDisplay();
  m_display->setTextSize(1);
  m_display->setCursor(2, 4);
  m_display->print("Saving...");
  m_display->display();
  delay(1000);
}