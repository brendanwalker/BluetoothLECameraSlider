// Adapted from: https://learn.sparkfun.com/tutorials/esp32-thing-plus-hookup-guide/arduino-example-esp32-ble
#include <Arduino.h>
#include "BLECharacteristic.h"
#include "BLEManager.h"
#include "BLE2904.h"
#include "ConfigManager.h"
#include "SliderManager.h"

#include <sstream>

#define SERVICE_UUID        "6b42e290-28cb-11ee-be56-0242ac120002"
#define COMMAND_CHARACTERISTIC_UUID "62c6b5d1-d304-4b9c-b12b-decd1e5b3614"
#define EVENT_CHARACTERISTIC_UUID "5c88bae1-db64-4483-a0f3-6b6786c6c145"
#define SLIDER_POS_CHARACTERISTIC_UUID "87b8f554-28cb-11ee-be56-0242ac120002"
#define SLIDER_SPEED_CHARACTERISTIC_UUID "781ef5a1-e5df-411d-9276-7a229e469719"
#define SLIDER_ACCEL_CHARACTERISTIC_UUID "5cf074e3-2014-4776-8734-b0d0eb49229a"
#define PAN_POS_CHARACTERISTIC_UUID "a86268cb-73bb-497c-bb9b-cf7af318919f"
#define PAN_SPEED_CHARACTERISTIC_UUID "838a79d5-c5f2-40ea-8f3d-c9d6fde08f56"
#define PAN_ACCEL_CHARACTERISTIC_UUID "4049db30-2096-460a-821f-05380dc37212"
#define TILT_POS_CHARACTERISTIC_UUID "9881b453-6636-4a73-a335-11bc737f6812"
#define TILT_SPEED_CHARACTERISTIC_UUID "d40730f2-7be2-425a-a70c-58d56f8d2295"
#define TILT_ACCEL_CHARACTERISTIC_UUID "885bee55-f069-4077-b18f-5cdb8b4a7003"

BLEManager* BLEManager::s_instance= nullptr;

BLEManager::BLEManager(ConfigManager* config)
  : m_config(config)
{
  s_instance= this;
}

void BLEManager::setup()
{
  Serial.println(F("BLEDevice::createServer"));
  BLEDevice::init("Camera Slider");
  m_pServer = BLEDevice::createServer();

  m_pService = m_pServer->createService(SERVICE_UUID);

  SliderState* silderManager= SliderState::getInstance();

  m_pCommandCharacteristic= makeUTF8Characteristic(COMMAND_CHARACTERISTIC_UUID, true, false, "");
  m_pEventCharacteristic= makeUTF8Characteristic(EVENT_CHARACTERISTIC_UUID, false, true, "");
  
  m_pSliderPosCharacteristic= makeFloatCharacteristic(SLIDER_POS_CHARACTERISTIC_UUID, true, silderManager->getSliderPosFraction());
  m_pSliderSpeedCharacteristic= makeFloatCharacteristic(SLIDER_SPEED_CHARACTERISTIC_UUID, true, silderManager->getSliderSpeedFraction());
  m_pSliderAccelCharacteristic= makeFloatCharacteristic(SLIDER_ACCEL_CHARACTERISTIC_UUID, true, silderManager->getSliderAccelFraction());

  m_pPanPosCharacteristic= makeFloatCharacteristic(PAN_POS_CHARACTERISTIC_UUID, true, silderManager->getPanPosFraction());
  m_pPanSpeedCharacteristic= makeFloatCharacteristic(PAN_SPEED_CHARACTERISTIC_UUID, true, silderManager->getPanSpeedFraction());
  m_pPanAccelCharacteristic= makeFloatCharacteristic(PAN_ACCEL_CHARACTERISTIC_UUID, true, silderManager->getPanAccelFraction());

  m_pTiltPosCharacteristic= makeFloatCharacteristic(TILT_POS_CHARACTERISTIC_UUID, true, silderManager->getTiltPosFraction());
  m_pTiltSpeedCharacteristic= makeFloatCharacteristic(TILT_SPEED_CHARACTERISTIC_UUID, true, silderManager->getTiltSpeedFraction());
  m_pTiltAccelCharacteristic= makeFloatCharacteristic(TILT_ACCEL_CHARACTERISTIC_UUID, true, silderManager->getTiltAccelFraction());    

  m_pService->start();

  Serial.println(F("Start Advertising BluetoothLE service"));
  m_pAdvertising = BLEDevice::getAdvertising();
  m_pAdvertising->setMinPreferred(0x06);  // functions that help with iPhone connections issue
  m_pAdvertising->setMinPreferred(0x12);  

  BLEDevice::startAdvertising();
}

void BLEManager::setCommandHandler(BLECommandHandler *handler)
{
  m_commandHandler= handler;
}

void BLEManager::clearCommandHandler(BLECommandHandler *handler)
{
  if (m_commandHandler == handler)
  {
    handler= nullptr;
  }
}

void BLEManager::sendEvent(const std::string& event)
{
  m_pEventCharacteristic->setValue(event.c_str());
  m_pEventCharacteristic->notify();
}

BLECharacteristic *BLEManager::makeFloatCharacteristic(const char* UUID, bool bWritable, float initialValue)
{
  uint32_t properties= BLECharacteristic::PROPERTY_READ;
  
  if (bWritable)
    properties|= BLECharacteristic::PROPERTY_WRITE;

  BLECharacteristic* pCharacteristic = m_pService->createCharacteristic(UUID, properties);

  // https://btprodspecificationrefs.blob.core.windows.net/assigned-numbers/Assigned%20Number%20Types/Assigned_Numbers.pdf
  // 0x2904 - Characteristic Presentation Format
  BLE2904 *pDescriptor= new BLE2904();
  pDescriptor->setDescription(BLE2904::FORMAT_FLOAT32);

  pCharacteristic->addDescriptor(pDescriptor);
  pCharacteristic->setCallbacks(this);
  
  if (bWritable)
    pCharacteristic->setValue(initialValue);

  return pCharacteristic;
}

BLECharacteristic *BLEManager::makeUTF8Characteristic(const char* UUID, bool bWritable, bool bNotifyOnChange, const char* initialValue)
{
  uint32_t properties= BLECharacteristic::PROPERTY_READ;
  
  if (bWritable)
    properties|= BLECharacteristic::PROPERTY_WRITE;

  if (bNotifyOnChange)
  {
    properties|= BLECharacteristic::PROPERTY_NOTIFY;
    properties|= BLECharacteristic::PROPERTY_INDICATE; // INDICATE == reliable notify
  }    

  BLECharacteristic* pCharacteristic = m_pService->createCharacteristic(UUID, properties);

  // https://btprodspecificationrefs.blob.core.windows.net/assigned-numbers/Assigned%20Number%20Types/Assigned_Numbers.pdf
  // 0x2904 - Characteristic Presentation Format
  BLE2904 *pDescriptor= new BLE2904();
  pDescriptor->setDescription(BLE2904::FORMAT_UTF8);

  pCharacteristic->addDescriptor(pDescriptor);
  pCharacteristic->setCallbacks(this);
  
  if (bWritable)
    pCharacteristic->setValue(initialValue);

  return pCharacteristic;
}

void BLEManager::onRead(BLECharacteristic* pCharacteristic, esp_ble_gatts_cb_param_t* param)
{
  float value= 0.f;

  // Slider Controls
  if (pCharacteristic == m_pSliderPosCharacteristic)
  {
    value= SliderState::getInstance()->getSliderPosFraction();
  }
  else if (pCharacteristic == m_pSliderSpeedCharacteristic)
  {
    value= SliderState::getInstance()->getSliderSpeedFraction();
  }
  else if (pCharacteristic == m_pSliderAccelCharacteristic)
  {
    value= SliderState::getInstance()->getSliderAccelFraction();
  }
  // Pan Controls
  else if (pCharacteristic == m_pPanPosCharacteristic)
  {
    value= SliderState::getInstance()->getPanPosFraction();
  }
  else if (pCharacteristic == m_pPanSpeedCharacteristic)
  {
    value= SliderState::getInstance()->getPanSpeedFraction();
  }
  else if (pCharacteristic == m_pPanAccelCharacteristic)
  {
    value= SliderState::getInstance()->getPanAccelFraction();
  }  
  // Tilt Controls
  else if (pCharacteristic == m_pTiltPosCharacteristic)
  {
    value= SliderState::getInstance()->getTiltPosFraction();
  }
  else if (pCharacteristic == m_pTiltSpeedCharacteristic)
  {
    value= SliderState::getInstance()->getTiltSpeedFraction();
  }
  else if (pCharacteristic == m_pTiltAccelCharacteristic)
  {
    value= SliderState::getInstance()->getTiltAccelFraction();
  }
  else
  {
    return;
  }

  pCharacteristic->setValue(value);
}

float BLEManager::readFloat(BLECharacteristic *pCharacteristic) 
{
	if (pCharacteristic->getLength() >= 4)
  {
    return *(float*)(pCharacteristic->getData());
	}

	return 0.0;
}

void BLEManager::onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t* param) 
{
  if (pCharacteristic == m_pCommandCharacteristic)
  {
    std::string value = pCharacteristic->getValue();

    if (value == "ping")
    {
      Serial.printf("BLEManager - Received Ping\n");
    }
    else if (m_commandHandler != nullptr)
    {
      Serial.printf("BLEManager - Handling command: %s\n", value.c_str());
      m_commandHandler->onCommand(value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring command: %s\n", value.c_str());
    }
  }
  // Slider Controls
  else if (pCharacteristic == m_pSliderPosCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setSliderPosFraction(value);
      Serial.printf("BLEManager - Setting Slider Pos fraction to: %f");
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Slider Pos control");
    }
  }
  else if (pCharacteristic == m_pSliderSpeedCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setSliderSpeedFraction(value);
      Serial.printf("BLEManager - Setting Slider Speed fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Slider Speed control");
    }
  }
  else if (pCharacteristic == m_pSliderAccelCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setSliderAccelFraction(value);
      Serial.printf("BLEManager - Setting Slider Accel fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Slider Accel control");
    }
  }
  // Pan Controls
  else if (pCharacteristic == m_pPanPosCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setPanPosFraction(value);
      Serial.printf("BLEManager - Setting Pan Pos fraction to: %f");
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Pan Pos control");
    }
  }
  else if (pCharacteristic == m_pPanSpeedCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setPanSpeedFraction(value);
      Serial.printf("BLEManager - Setting Pan Speed fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Pan Speed control");
    }
  }
  else if (pCharacteristic == m_pPanAccelCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setPanAccelFraction(value);
      Serial.printf("BLEManager - Setting Pan Accel fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Pan Accel control");
    }
  }  
  // Tilt Controls
  else if (pCharacteristic == m_pTiltPosCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setTiltPosFraction(value);
      Serial.printf("BLEManager - Setting Tilt Pos fraction to: %f");
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Tilt Pos control");
    }
  }
  else if (pCharacteristic == m_pTiltSpeedCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setTiltSpeedFraction(value);
      Serial.printf("BLEManager - Setting Tilt Speed fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Tilt Speed control");
    }
  }
  else if (pCharacteristic == m_pTiltAccelCharacteristic)
  {
    float value = readFloat(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setTiltAccelFraction(value);
      Serial.printf("BLEManager - Setting Tilt Accel fraction to: %f", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Tilt Accel control");
    }
  }      
}