// Adapted from: https://learn.sparkfun.com/tutorials/esp32-thing-plus-hookup-guide/arduino-example-esp32-ble
#include <Arduino.h>
#include "BLECharacteristic.h"
#include "BLEManager.h"
#include "BLE2902.h"
#include "BLE2904.h"
#include "ConfigManager.h"
#include "SliderManager.h"

#include <sstream>
#include <vector>

#define SERVICE_UUID        "6b42e290-28cb-11ee-be56-0242ac120002"
#define STATUS_CHARACTERISTIC_UUID "6b390b6c-b306-4ccb-a126-d759c5113177"
#define REQUEST_CHARACTERISTIC_UUID "62c6b5d1-d304-4b9c-b12b-decd1e5b3614"
#define RESPONSE_CHARACTERISTIC_UUID "5c88bae1-db64-4483-a0f3-6b6786c6c145"
#define SLIDER_POS_CHARACTERISTIC_UUID "87b8f554-28cb-11ee-be56-0242ac120002"
#define PAN_POS_CHARACTERISTIC_UUID "a86268cb-73bb-497c-bb9b-cf7af318919f"
#define TILT_POS_CHARACTERISTIC_UUID "9881b453-6636-4a73-a335-11bc737f6812"
#define SPEED_CHARACTERISTIC_UUID "781ef5a1-e5df-411d-9276-7a229e469719"
#define ACCEL_CHARACTERISTIC_UUID "5cf074e3-2014-4776-8734-b0d0eb49229a"

BLEManager* BLEManager::s_instance= nullptr;

BLEManager::BLEManager(ConfigManager* config)
  : m_config(config)
{
  s_instance= this;
}

void BLEManager::setup()
{
  Serial.println(F("BLEDevice::createServer"));

  // Create the BLE Device
  BLEDevice::init("Camera Slider");

  // Create the BLE Server and register connection callbacks
  m_pServer = BLEDevice::createServer();
  m_pServer->setCallbacks(this);

  // Create the BLE service
  m_pService = m_pServer->createService(BLEUUID(SERVICE_UUID), 32);

  // Create slider BLE Characteristics
  SliderState* silderManager= SliderState::getInstance();

  m_pStatusCharacteristic= makeUTF8OutputCharacteristic(STATUS_CHARACTERISTIC_UUID);
  m_pRequestCharacteristic= makeUTF8InputCharacteristic(REQUEST_CHARACTERISTIC_UUID);
  m_pResponseCharacteristic= makeUTF8OutputCharacteristic(RESPONSE_CHARACTERISTIC_UUID);
  
  m_pSliderPosCharacteristic= makeInt32OutputCharacteristic(SLIDER_POS_CHARACTERISTIC_UUID, silderManager->getSlideStepperPosition());
  m_pPanPosCharacteristic= makeInt32OutputCharacteristic(PAN_POS_CHARACTERISTIC_UUID, silderManager->getPanStepperPosition());
  m_pTiltPosCharacteristic= makeInt32OutputCharacteristic(TILT_POS_CHARACTERISTIC_UUID, silderManager->getTiltStepperPosition());
  m_pSpeedCharacteristic= makeFloatInputCharacteristic(SPEED_CHARACTERISTIC_UUID, silderManager->getSpeedFraction());
  m_pAccelCharacteristic= makeFloatInputCharacteristic(ACCEL_CHARACTERISTIC_UUID, silderManager->getAccelFraction());    

  // Start the service
  m_pService->start();

  // Start advertising
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  BLEDevice::startAdvertising();
  Serial.println(F("Start Advertising BluetoothLE service"));

  // Listen to motor events
  silderManager->setListener(this);
}

void BLEManager::loop()
{
  // Restart advertising on disconnection
  if (m_wasDeviceConnected && !m_isDeviceConnected)
  {
    m_pServer->startAdvertising();
    Serial.println("BLEManager - Restart advertising");    
  }
  m_wasDeviceConnected= m_isDeviceConnected;
}

void BLEManager::onSliderMinSet(int32_t pos)
{
  if (m_isDeviceConnected)
  {
    Serial.printf("Send slide_min_set %d\n", pos);
  }
}

void BLEManager::onSliderMaxSet(int32_t pos)
{
  if (m_isDeviceConnected)
  {
    Serial.printf("Send slide_max_set %d", pos);
  }
}

void BLEManager::onSliderTargetSet(int32_t pos)
{
  if (m_isDeviceConnected)
  {
    Serial.println("Update Slider Pos");
    m_pSliderPosCharacteristic->setValue((uint8_t*)&pos, 4);
    m_pSliderPosCharacteristic->notify();
  }
}

void BLEManager::onPanTargetSet(int32_t pos)
{
  if (m_isDeviceConnected)
  {
    Serial.println("Update Pan Pos");
    m_pPanPosCharacteristic->setValue((uint8_t*)&pos, 4);
    m_pPanPosCharacteristic->notify();
  }
}

void BLEManager::onTiltTargetSet(int32_t pos)
{
  if (m_isDeviceConnected)
  {
    Serial.println("Update Tilt Pos");
    m_pTiltPosCharacteristic->setValue((uint8_t*)&pos, 4);
    m_pTiltPosCharacteristic->notify();
  }
}

void BLEManager::onMoveToTargetStart()
{
  if (m_isDeviceConnected)
  {
    Serial.println("Send move_start");
    setStatus("move_start");
  }
}

void BLEManager::onMoveToTargetComplete()
{
  if (m_isDeviceConnected)
  {
    Serial.println("Send move_complete");
    setStatus("move_complete");
  }
}

void BLEManager::onConnect(BLEServer *pServer) 
{
  Serial.printf("BLEManager - Device Connected\n");
  m_isDeviceConnected= true;

  Serial.printf("BLEManager - Restart Advertising BluetoothLE service\n");
  BLEDevice::startAdvertising();
}

void BLEManager::onDisconnect(BLEServer *pServer)
{
  Serial.printf("BLEManager - Device Disconnected\n");
  m_isDeviceConnected= false; 
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

void BLEManager::setStatus(const std::string& event)
{
  m_pStatusCharacteristic->setValue(event.c_str());
  m_pStatusCharacteristic->notify();
}

BLECharacteristic *BLEManager::makeFloatInputCharacteristic(const char* UUID, float initialValue)
{
  BLECharacteristic* pCharacteristic = m_pService->createCharacteristic(
      UUID,       
      BLECharacteristic::PROPERTY_READ |
      BLECharacteristic::PROPERTY_WRITE);

  // https://btprodspecificationrefs.blob.core.windows.net/assigned-numbers/Assigned%20Number%20Types/Assigned_Numbers.pdf
  // 0x2904 - Characteristic Presentation Format
  BLE2904 *pDescriptor= new BLE2904();
  pDescriptor->setDescription(BLE2904::FORMAT_FLOAT32);

  pCharacteristic->addDescriptor(pDescriptor);
  pCharacteristic->setCallbacks(this);
  pCharacteristic->setValue(initialValue);

  return pCharacteristic;
}

BLECharacteristic *BLEManager::makeInt32OutputCharacteristic(const char* UUID, int32_t initialValue)
{
  BLECharacteristic* pCharacteristic = m_pService->createCharacteristic(
      UUID,       
      BLECharacteristic::PROPERTY_READ   |
      BLECharacteristic::PROPERTY_WRITE  |
      BLECharacteristic::PROPERTY_NOTIFY |
      BLECharacteristic::PROPERTY_INDICATE);

  pCharacteristic->addDescriptor(new BLE2902());
  pCharacteristic->setCallbacks(this); 

  pCharacteristic->setValue((uint8_t*)&initialValue, 4);

  return pCharacteristic;
}

BLECharacteristic *BLEManager::makeUTF8OutputCharacteristic(const char* UUID)
{
  BLECharacteristic* pCharacteristic = 
    m_pService->createCharacteristic(
      UUID, 
      BLECharacteristic::PROPERTY_READ   |
      BLECharacteristic::PROPERTY_WRITE  |
      BLECharacteristic::PROPERTY_NOTIFY |
      BLECharacteristic::PROPERTY_INDICATE);
  pCharacteristic->addDescriptor(new BLE2902());

  return pCharacteristic;
}

BLECharacteristic *BLEManager::makeUTF8InputCharacteristic(const char* UUID)
{
  BLECharacteristic* pCharacteristic = 
    m_pService->createCharacteristic(
      UUID, 
      BLECharacteristic::PROPERTY_WRITE);

  pCharacteristic->setCallbacks(this);  

  return pCharacteristic;
}

void BLEManager::onRead(BLECharacteristic* pCharacteristic, esp_ble_gatts_cb_param_t* param)
{
  // Position Controls
  if (pCharacteristic == m_pSliderPosCharacteristic)
  {
    int32_t value= SliderState::getInstance()->getSlideStepperPosition();
    m_pSliderPosCharacteristic->setValue((uint8_t*)&value, 4);
  }
  else if (pCharacteristic == m_pPanPosCharacteristic)
  {
    int32_t value= SliderState::getInstance()->getPanStepperPosition();
    m_pPanPosCharacteristic->setValue((uint8_t*)&value, 4);
  }
  else if (pCharacteristic == m_pTiltPosCharacteristic)
  {
    int32_t value= SliderState::getInstance()->getTiltStepperPosition();
    m_pTiltPosCharacteristic->setValue((uint8_t*)&value, 4);
  }
  // Speed Controls
  else if (pCharacteristic == m_pSpeedCharacteristic)
  {
    float value= SliderState::getInstance()->getSpeedFraction();
    m_pSpeedCharacteristic->setValue((uint8_t*)&value, 4);
  }
  else if (pCharacteristic == m_pAccelCharacteristic)
  {
    float value= SliderState::getInstance()->getAccelFraction();
    m_pAccelCharacteristic->setValue((uint8_t*)&value, 4);
  }
  else
  {
    return;
  }
}

float BLEManager::getFloatCharacteristicValue(BLECharacteristic *pCharacteristic) 
{
  int byteCount= pCharacteristic->getLength();
  u_int8_t* byteArray= pCharacteristic->getData();
	if (byteCount >= 4)
  {
    Serial.printf("BLEManager - readFloat(%d bytes) =", byteCount);
    for (int byteIndex= 0; byteIndex < byteCount; ++byteIndex)
    {
      Serial.printf("%c0x%x", byteIndex > 0 ? ',' : ' ', byteArray[byteIndex]);
    }

    union {
        byte b[4];
        float f;
    } value;
    value.b[0] = byteArray[0];
    value.b[1] = byteArray[1];
    value.b[2] = byteArray[2];
    value.b[3] = byteArray[3];
    Serial.printf(" => %f", value.f);

    Serial.println();

    return value.f;
	}

	return 0.0;
}

void BLEManager::onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t* param) 
{
  if (pCharacteristic == m_pRequestCharacteristic)
  {
    std::string value = pCharacteristic->getValue();
    
    // Extract command args
    std::vector<std::string> args;
    std::istringstream command_stream(value);
    for (std::string arg; getline(command_stream, arg, ' '); )
    {
      args.push_back(arg);
    }

    // Extract the command Id (if any) from the first arg
    std::string commandIdStr;
    int commandId= 0;    
    if (args.size() > 0)
    {
      // Extract command id from the front of the args
      commandIdStr= args[0];
      commandId= std::atoi(commandIdStr.c_str());

      // Remove the command Id from the args list
      args.erase(args.begin());
    }

    // Prepend the command id (if any) to the start of the results
    std::vector<std::string> results;
    if (commandId > 0)
    {
      results.push_back(commandIdStr);
    }

    // Process the command
    if (args.size() > 0 && args[0] == "ping")
    {
      Serial.printf("BLEManager - Received Ping\n");
      results.push_back("pong");
    }
    else if (m_commandHandler != nullptr)
    {
      Serial.printf("BLEManager - Handling command: %s\n", value.c_str());

      if (args.size() > 0)
      {
        m_commandHandler->onCommand(args, results);
      }
    }
    else
    {
      Serial.printf("BLEManager - Ignoring command: %s\n", value.c_str());
    }

    // always send a response back if we had a request id
    // so that the clinet knows it's safe to post another command
    // without it getting missed
    if (results.size() > 0)
    {
      // Join the results into a single string with " " seperated elements
      std::stringstream ss;      
      ss << results[0];
      for (size_t i = 1; i < results.size(); i++)
      {
        ss << " " << results[i];
      }

      std::string response= ss.str();
      m_pResponseCharacteristic->setValue(response.c_str());
      m_pResponseCharacteristic->notify();      
    }
  }
  // Speed Controls
  else if (pCharacteristic == m_pSpeedCharacteristic)
  {
    float value = getFloatCharacteristicValue(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setSpeedFraction(value);
      Serial.printf("BLEManager - Setting Speed fraction to: %f\n", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Speed control\n");
    }
  }
  else if (pCharacteristic == m_pAccelCharacteristic)
  {
    float value = getFloatCharacteristicValue(pCharacteristic);

    if (m_bleControlEnabled)
    {
      SliderState::getInstance()->setAccelFraction(value);
      Serial.printf("BLEManager - Setting Accel fraction to: %f\n", value);
    }
    else
    {
      Serial.printf("BLEManager - Ignoring Accel control");
    }
  }      
}