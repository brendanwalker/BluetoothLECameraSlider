#include "NetworkManager.h"

NetworkManager* NetworkManager::s_instance= nullptr;

NetworkManager::NetworkManager(ConfigManager* config)
  : m_config(config)
  , m_artnet()
{
  s_instance= this;
}

bool NetworkManager::isWiFiConnected() const 
{ 
  return WiFi.status() == WL_CONNECTED;
}

void NetworkManager::setListener(NetworkEventListener *listener)
{
  m_listener= listener; 
}

void NetworkManager::clearListener(NetworkEventListener *listener)
{
  if (m_listener == listener)
    m_listener= nullptr;
}

void NetworkManager::setup()
{
}

void NetworkManager::loop()
{
  // Handle WiFi connection state changes
  int currentWiFiStatus= WiFi.status();
  if (m_previousWiFiStatus != WL_CONNECTED && currentWiFiStatus == WL_CONNECTED)
  {
    Serial.println("Connected to Wifi network");
    onWifiConnected();
  }
  else if (m_previousWiFiStatus == WL_CONNECTED && currentWiFiStatus != WL_CONNECTED)
  {
    Serial.println("Disconnected to Wifi network");
    onWifiDisconnected();
  }

  // Poll Artnet state when connected to wifi
  if (currentWiFiStatus == WL_CONNECTED)
  {
    m_artnet.parse();
  }

  // Keep track of previous connection state to de-bounce state changes
  m_previousWiFiStatus= currentWiFiStatus;
}

const char* NetworkManager::WlStatusToStr(wl_status_t wlStatus)
{
	switch (wlStatus)
	{
	case WL_NO_SHIELD: return "WL_NO_SHIELD";
	case WL_IDLE_STATUS: return "WL_IDLE_STATUS";
	case WL_NO_SSID_AVAIL: return "WL_NO_SSID_AVAIL";
	case WL_SCAN_COMPLETED: return "WL_SCAN_COMPLETED";
	case WL_CONNECTED: return "WL_CONNECTED";
	case WL_CONNECT_FAILED: return "WL_CONNECT_FAILED";
	case WL_CONNECTION_LOST: return "WL_CONNECTION_LOST";
	case WL_DISCONNECTED: return "WL_DISCONNECTED";
	default: return "Unknown";
	}
}

void NetworkManager::tryConnectToWiFi(float timeoutSeconds)
{
  if (!isWiFiConnected())
  {
    ConfigManager* configManager= ConfigManager::getInstance();

    if (configManager->hasSSID())
    {
      const char* ssid= configManager->getSSID();
      const char* password= configManager->getPassword();
      Serial.printf("tryConnectToWiFi: SSID(%s), Password(%s)\n", ssid, password);

      WiFi._setStatus(WL_NO_SHIELD);
      WiFi.begin(ssid, password);

      unsigned long timeoutMillis= (unsigned long)(timeoutSeconds*1000.f);
      WiFi.waitForConnectResult(timeoutMillis);
    }
  }
}

void NetworkManager::onWifiConnected()
{
  // Setup ArtNet wifi connection manager
  m_artnet.begin(); 

  if (m_listener != nullptr)
    m_listener->onWifiConnected();  
}

void NetworkManager::onWifiDisconnected()
{
  m_artnet.clear_subscribers();

  if (m_listener != nullptr)
    m_listener->onWifiDisconnected();
}