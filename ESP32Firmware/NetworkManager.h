#ifndef NetworkManager_h
#define NetworkManager_h

#include "Arduino.h"
#include "ConfigManager.h"
#include <ArtnetWiFi.h>

class NetworkEventListener
{
public:
  virtual void onWifiConnected() {}
  virtual void onWifiDisconnected() {}
};

class NetworkManager
{
public:
  NetworkManager(ConfigManager* config);

  static NetworkManager* getInstance() { return s_instance; }

  bool isWiFiConnected() const;
  ArtnetWiFiReceiver& getArtnetReceiver() { return m_artnet; }

  void setListener(NetworkEventListener *listener);
  void clearListener(NetworkEventListener *listener);  

  static const char* WlStatusToStr(wl_status_t wlStatus);
  void tryConnectToWiFi(float timeoutSeconds);

  void setup();
  void loop();

private:

  static NetworkManager* s_instance;

  ConfigManager* m_config= nullptr;

  // connection state
  int m_previousWiFiStatus= WL_DISCONNECTED; 

  // Listener
  NetworkEventListener* m_listener= nullptr;  

  // ArtNet/DMX512 receiver
  ArtnetWiFiReceiver m_artnet;

  // Wifi Events
  void onWifiConnected();
  void onWifiDisconnected();
};

#endif 