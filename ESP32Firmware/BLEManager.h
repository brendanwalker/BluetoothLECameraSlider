#ifndef BLE_MANAGER_H__
#define BLE_MANAGER_H__

#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include "ConfigManager.h"

class BLECommandHandler
{
public:
  virtual void onCommand(const std::string& command) {}
};

class BLEManager : public BLECharacteristicCallbacks, public BLEServerCallbacks
{
public:
  BLEManager(ConfigManager* config);
  static BLEManager* getInstance() { return s_instance; }

  void setBLEControlEnabled(bool bEnabled) { m_bleControlEnabled= bEnabled; }
  void setCommandHandler(BLECommandHandler *handler);
  void clearCommandHandler(BLECommandHandler *handler);
  void sendEvent(const std::string& event);

  void setup();

private:
  static BLEManager* s_instance;

  BLECommandHandler* m_commandHandler= nullptr;

  ConfigManager* m_config= nullptr;

  // BLEServerCallbacks
  void onConnect(BLEServer *pServer) override;
  void onDisconnect(BLEServer *pServer) override;

  // BLECharacteristicCallbacks
  BLECharacteristic *makeFloatCharacteristic(const char* UUID, bool bWritable, float initialValue);
  BLECharacteristic *makeUTF8OutputCharacteristic(const char* UUID);
  BLECharacteristic *makeUTF8InputCharacteristic(const char* UUID);
  float getFloatCharacteristicValue(BLECharacteristic *pCharacteristic);
  void onRead(BLECharacteristic* pCharacteristic, esp_ble_gatts_cb_param_t* param) override;
  void onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t* param) override;

  BLEServer *m_pServer= nullptr;
  BLEService *m_pService= nullptr;
  BLECharacteristic *m_pCommandCharacteristic= nullptr;
  BLECharacteristic *m_pEventCharacteristic= nullptr;
  BLECharacteristic *m_pSliderPosCharacteristic= nullptr;
  BLECharacteristic *m_pSliderSpeedCharacteristic= nullptr;
  BLECharacteristic *m_pSliderAccelCharacteristic= nullptr;
  BLECharacteristic *m_pPanPosCharacteristic= nullptr;
  BLECharacteristic *m_pPanSpeedCharacteristic= nullptr;
  BLECharacteristic *m_pPanAccelCharacteristic= nullptr;
  BLECharacteristic *m_pTiltPosCharacteristic= nullptr;
  BLECharacteristic *m_pTiltSpeedCharacteristic= nullptr;
  BLECharacteristic *m_pTiltAccelCharacteristic= nullptr;    
  BLEAdvertising *m_pAdvertising= nullptr;

  bool m_bleControlEnabled= true;
  int m_deviceConnectedCount= 0;
};

#endif // BLE_MANAGER_H__