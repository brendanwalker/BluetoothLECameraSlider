// Adapted from: https://learn.sparkfun.com/tutorials/esp32-thing-plus-hookup-guide/arduino-example-esp32-ble
#include <Arduino.h>
#include "BLECharacteristic.h"
#include "BLEManager.h"
#include "BLE2904.h"
#include "ConfigManager.h"
#include <sstream>

#define SERVICE_UUID        "6b42e290-28cb-11ee-be56-0242ac120002"
#define SSID_CHARACTERISTIC_UUID "87b8f554-28cb-11ee-be56-0242ac120002"
#define PASSWORD_CHARACTERISTIC_UUID "781ef5a1-e5df-411d-9276-7a229e469719"
#define IP_CHARACTERISTIC_UUID "5cf074e3-2014-4776-8734-b0d0eb49229a"
#define GATEWAY_CHARACTERISTIC_UUID "a86268cb-73bb-497c-bb9b-cf7af318919f"
#define SUBNET_CHARACTERISTIC_UUID "838a79d5-c5f2-40ea-8f3d-c9d6fde08f56"

BLEManager::BLEManager(ConfigManager* config)
  : m_config(config)
{
}

void BLEManager::setup()
{
  Serial.println(F("BLEDevice::createServer"));
  BLEDevice::init("Camera Slider");
  m_pServer = BLEDevice::createServer();

  m_pService = m_pServer->createService(SERVICE_UUID);
  
  m_pSSIDCharacteristic= makeUTF8Characteristic(SSID_CHARACTERISTIC_UUID, true, m_config->getSSID());
  m_pPasswordCharacteristic= makeUTF8Characteristic(PASSWORD_CHARACTERISTIC_UUID, true, m_config->getPassword());

  String ipString= m_config->getIPAddress().toString();
  m_pIPCharacteristic= makeUTF8Characteristic(IP_CHARACTERISTIC_UUID, false, ipString.c_str());

  String gatewayString= m_config->getGateway().toString();
  m_pGatewayCharacteristic= makeUTF8Characteristic(GATEWAY_CHARACTERISTIC_UUID, false, gatewayString.c_str());

  String subnetString= m_config->getSubnet().toString();
  m_pSubnetCharacteristic= makeUTF8Characteristic(SUBNET_CHARACTERISTIC_UUID, false, subnetString.c_str());  

  m_pService->start();

  Serial.println(F("Start Advertising BluetoothLE service"));
  m_pAdvertising = BLEDevice::getAdvertising();
  m_pAdvertising->setMinPreferred(0x06);  // functions that help with iPhone connections issue
  m_pAdvertising->setMinPreferred(0x12);  

  BLEDevice::startAdvertising();
}

BLECharacteristic *BLEManager::makeUTF8Characteristic(const char* UUID, bool bWritable, const char* initialValue)
{
  uint32_t properties= BLECharacteristic::PROPERTY_READ;
  
  if (bWritable)
    properties|= BLECharacteristic::PROPERTY_WRITE;

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
 		std::ostringstream os;

    if (pCharacteristic == m_pSSIDCharacteristic)
    {
      os << m_config->getSSID();
    }
    else if (pCharacteristic == m_pPasswordCharacteristic)
    {
      os << m_config->getPassword();
    }  
    else if (pCharacteristic == m_pIPCharacteristic)
    {
      os << m_config->getIPAddress().toString();
    }
    else if (pCharacteristic == m_pGatewayCharacteristic)
    {
      os << m_config->getGateway().toString();
    }
    else if (pCharacteristic == m_pSubnetCharacteristic)
    {
      os << m_config->getSubnet().toString();
    } 
    
    pCharacteristic->setValue(os.str());
}

void BLEManager::onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t* param) 
{
  std::string value = pCharacteristic->getValue();

  if (pCharacteristic == m_pSSIDCharacteristic)
  {
    Serial.printf("BLEManager - Writing SSID: %s\n", value.c_str());
    m_config->setSSID(value.c_str());
  }
  else if (pCharacteristic == m_pPasswordCharacteristic)
  {
    Serial.printf("BLEManager - Writing Password: %s\n", value.c_str());
    m_config->setPassword(value.c_str());
  }  
}