#ifndef BLE_MANAGER_H__
#define BLE_MANAGER_H__

#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include "ConfigManager.h"

class BLEManager: public BLECharacteristicCallbacks 
{
public:
  BLEManager(ConfigManager* config);
  void setup();

private:
  ConfigManager* m_config= nullptr;

  BLECharacteristic *makeUTF8Characteristic(const char* UUID, bool bWritable, const char* initialValue);
  void onRead(BLECharacteristic* pCharacteristic, esp_ble_gatts_cb_param_t* param) override;
  void onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t* param) override;

  BLEServer *m_pServer= nullptr;
  BLEService *m_pService= nullptr;
  BLECharacteristic *m_pSSIDCharacteristic= nullptr;
  BLECharacteristic *m_pPasswordCharacteristic= nullptr;
  BLECharacteristic *m_pIPCharacteristic= nullptr;
  BLECharacteristic *m_pGatewayCharacteristic= nullptr;
  BLECharacteristic *m_pSubnetCharacteristic= nullptr;
  BLEAdvertising *m_pAdvertising= nullptr;
};

#endif // BLE_MANAGER_H__