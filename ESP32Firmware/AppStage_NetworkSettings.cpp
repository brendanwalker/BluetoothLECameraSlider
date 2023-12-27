#include "Arduino.h"
#include "App.h"
#include "AppStage_NetworkSettings.h"
#include "ConfigManager.h"
#include "NetworkManager.h"
#include "RotaryEncoder.h"
#include <WiFi.h>
#include <Adafruit_SSD1306.h>

#define WIFI_CONNECTION_TIMEOUT         10.f

enum class eRetryBackOptions : int
{
  Retry,
  Back,

  COUNT
};
static const String kRetryBackStrings[(int)eRetryBackOptions::COUNT] = {"Retry", "Back"};

enum class eYesNoOptions : int
{
  Yes,
  No,

  COUNT
};
static const String kYesNoStrings[(int)eYesNoOptions::COUNT] = {"Yes", "No"};

const char* AppStage_NetworkSettings::APP_STAGE_NAME = "NetworkSettings";
AppStage_NetworkSettings* AppStage_NetworkSettings::s_instance= nullptr;

AppStage_NetworkSettings::AppStage_NetworkSettings(App* app)
	: AppStage(app, AppStage_NetworkSettings::APP_STAGE_NAME)
{ 
  s_instance= this;
}

void AppStage_NetworkSettings::enter()
{
  Serial.println("Enter Network Settings");

  AppStage::enter();

  ConfigManager::getInstance()->setListener(this);

  eNetworkSettingsStep initialStep= eNetworkSettingsStep::INVALID;
  if (NetworkManager::getInstance()->isWiFiConnected())
  {
    Serial.println("WiFi already connected");
    initialStep= eNetworkSettingsStep::Connected;
  }
  else
  {
    ConfigManager* configManager= ConfigManager::getInstance();

    if (configManager->hasSSID())
    {
      const char* ssid= configManager->getSSID();
      Serial.printf("Connect to existing ssid: %s\n", ssid);

      initialStep= eNetworkSettingsStep::Connecting;
    }
    else
    {
      Serial.println("Scan for wireless network SSIDs");

      initialStep= eNetworkSettingsStep::ScanSSID;
    }
  }

  m_step= eNetworkSettingsStep::INVALID;
  setState(initialStep);
}

void AppStage_NetworkSettings::exit()
{
  onLeaveStep(m_step);
  ConfigManager::getInstance()->clearListener(this);
  m_bAutoExitOnConnect= false;
  AppStage::exit();

  Serial.println("Exit Network Settings");
}

void AppStage_NetworkSettings::onPasswordChanged()
{
  if (m_step == eNetworkSettingsStep::WaitForPassword)
  {
    ConfigManager* configManager= ConfigManager::getInstance();
    const char* password= configManager->getPassword();
    Serial.printf("Password Set: %s\n", password);

    setState(eNetworkSettingsStep::Connecting);
  }
}

// Selection Events
void AppStage_NetworkSettings::onOptionClicked(int optionIndex)
{
  if (m_step == eNetworkSettingsStep::VerifySSID)
  {
    switch((eYesNoOptions)optionIndex)
    {
      case eYesNoOptions::Yes:
        setState(eNetworkSettingsStep::VerifyPassword);
        break;
      case eYesNoOptions::No:
        setState(eNetworkSettingsStep::ScanSSID);
        break;        
    }
  }
  else if (m_step == eNetworkSettingsStep::SelectSSID)
  {   
    const String& ssidString= m_ssidStrings[optionIndex];

    // Save the selected SSID
    ConfigManager* configManager= ConfigManager::getInstance();
    configManager->setSSID(ssidString.c_str());

    // Next verify password for the wifi network
    setState(eNetworkSettingsStep::VerifySSID);
  }
  else if (m_step == eNetworkSettingsStep::FailedScanSSID)
  {
    switch((eRetryBackOptions)optionIndex)
    {
      case eRetryBackOptions::Retry:
        setState(eNetworkSettingsStep::ScanSSID);
        break;
      case eRetryBackOptions::Back:
        m_app->popAppState();
        break;        
    }
  }
  else if (m_step == eNetworkSettingsStep::VerifyPassword)
  {
    switch((eYesNoOptions)optionIndex)
    {
      case eYesNoOptions::Yes:
        setState(eNetworkSettingsStep::Connecting);
        break;
      case eYesNoOptions::No:
        setState(eNetworkSettingsStep::WaitForPassword);
        break;        
    }
  }
  else if (m_step == eNetworkSettingsStep::Connected)
  {
    switch((eRetryBackOptions)optionIndex)
    {
      case eRetryBackOptions::Retry:
        setState(eNetworkSettingsStep::ScanSSID);
        break;        
      case eRetryBackOptions::Back:
        m_app->popAppState();
        break;
    }    
  }
  else if (m_step == eNetworkSettingsStep::FailedConnect)
  {
    switch((eRetryBackOptions)optionIndex)
    {
      case eRetryBackOptions::Retry:
        setState(eNetworkSettingsStep::Connecting);
        break;        
      case eRetryBackOptions::Back:
        setState(eNetworkSettingsStep::SelectSSID);
        break;
    }      
  }
}

void AppStage_NetworkSettings::setState(eNetworkSettingsStep newStep)
{
  if (newStep != m_step)
  {
    onLeaveStep(m_step);
    m_step= newStep;
    onEnterStep(newStep);
  }
}

void AppStage_NetworkSettings::onLeaveStep(eNetworkSettingsStep oldState)
{
  if (m_activeMenu != nullptr)
  {
    m_activeMenu->hide();
    delete m_activeMenu;
    m_activeMenu= nullptr;
  }
}

void AppStage_NetworkSettings::onEnterStep(eNetworkSettingsStep newStep)
{
  switch(newStep)
  {
  case eNetworkSettingsStep::ScanSSID:
    {
      Serial.println("Enter ScanSSID state");
      render();
      WiFi.scanNetworks(true);
    } break;
  case eNetworkSettingsStep::FailedScanSSID:
    {
      Serial.println("Enter FailedScanSSID state");
      m_activeMenu = new SelectionMenu("Scan failed", kRetryBackStrings, (int)eRetryBackOptions::COUNT);
    }
    break;
  case eNetworkSettingsStep::VerifySSID:
    {
      Serial.println("Enter VerifySSID state");

      ConfigManager* configManager= ConfigManager::getInstance();
      String ssidString= configManager->getSSID();

      m_activeMenu = new SelectionMenu("SSID Correct?", kYesNoStrings, (int)eYesNoOptions::COUNT);
      m_activeMenu->addHeader(ssidString);
    }
    break;
  case eNetworkSettingsStep::SelectSSID:
    {
      Serial.println("Enter SelectSSID state");

      m_activeMenu = new SelectionMenu("Select SSID", m_ssidStrings, m_ssidCount);
    }
    break;
  case eNetworkSettingsStep::VerifyPassword:
    {
      Serial.println("Enter VerifyPassword state");

      ConfigManager* configManager= ConfigManager::getInstance();
      String passwordString= configManager->getPassword();

      m_activeMenu = new SelectionMenu("Password Correct?", kYesNoStrings, (int)eYesNoOptions::COUNT);
      m_activeMenu->addHeader(passwordString);
    }  
    break;
  case eNetworkSettingsStep::WaitForPassword:
    Serial.println("Enter WaitForPassword state");
    break;
  case eNetworkSettingsStep::Connecting:
    {
      Serial.println("Enter Connecting state");
      render();

      m_connectionTimeout= WIFI_CONNECTION_TIMEOUT;
      NetworkManager::getInstance()->tryConnectToWiFi(WIFI_CONNECTION_TIMEOUT);
    }
    break;
  case eNetworkSettingsStep::Connected:
    {      
      Serial.println("Enter Connected state");

      ConfigManager* configManager= ConfigManager::getInstance();
      String ssidString= configManager->getSSID();
      String passwordString= configManager->getPassword();
      String ipString= configManager->getIPAddress().toString();
      String gatewaytString= configManager->getGateway().toString();
      String subnetString= configManager->getSubnet().toString();

      m_activeMenu = new SelectionMenu("Network Info", kRetryBackStrings, (int)eRetryBackOptions::COUNT);
      m_activeMenu->addHeader(ssidString);
      m_activeMenu->addHeader(passwordString);
      m_activeMenu->addHeader(ipString);
      m_activeMenu->addHeader(gatewaytString);
      m_activeMenu->addHeader(subnetString);
    }
    break;
  case eNetworkSettingsStep::FailedConnect:
    {
      Serial.println("Enter FailedConnect state");
      
      m_activeMenu = new SelectionMenu("Connect failed", kRetryBackStrings, (int)eRetryBackOptions::COUNT);

      // Clear this flag if we failed so we can verify connection info on successful retry
      m_bAutoExitOnConnect= false;
    }  
    break;
  }  

  if (m_activeMenu != nullptr)
  {
    m_activeMenu->setListener(this);
    m_activeMenu->show();
  }
}

void AppStage_NetworkSettings::update(float deltaSeconds)
{
  eNetworkSettingsStep nextStep= m_step;

  if (m_step == eNetworkSettingsStep::ScanSSID)
  {
    nextStep= update_scanSSID(deltaSeconds);
  }
  else if (m_step == eNetworkSettingsStep::Connecting)
  {
    nextStep= update_connecting(deltaSeconds);
  }

  if (nextStep != m_step)
  {
    setState(nextStep);
  }

  // Handle auto exit on connection last
  if (m_step == eNetworkSettingsStep::Connected && m_bAutoExitOnConnect)
  {
    m_app->popAppState();
  }
}

eNetworkSettingsStep AppStage_NetworkSettings::update_scanSSID(float deltaSeconds)
{
  Serial.println("update_scanSSID called");
  eNetworkSettingsStep nextStep= m_step;

  int result = WiFi.scanComplete();
  if (result == WIFI_SCAN_FAILED)
  {
    Serial.println("Failed SSID scan");
    nextStep= eNetworkSettingsStep::FailedScanSSID;
  }
  else if (result > 0)
  {
    Serial.printf("Found %d networks\n", result);

    // Clean up any previous networks
    if (m_ssidStrings != nullptr)
    {
      delete[] m_ssidStrings;
      m_ssidStrings= nullptr;
    }

    // Copy out the list of SSIDs found
    m_ssidCount= result;
    m_ssidStrings= new String[m_ssidCount];
    for (int i= 0; i < m_ssidCount; i++)
    {
        m_ssidStrings[i]= WiFi.SSID(i);
        Serial.printf("SSID(%d)=%s\n", i, m_ssidStrings[i].c_str());
    }
    Serial.println("Wifi scan delete");
    WiFi.scanDelete();
    
    if (m_ssidCount > 0)
      nextStep= eNetworkSettingsStep::SelectSSID;
    else
      nextStep= eNetworkSettingsStep::FailedScanSSID;
  }

  return nextStep;
}

eNetworkSettingsStep AppStage_NetworkSettings::update_connecting(float deltaSeconds)
{
  eNetworkSettingsStep nextStep= m_step;

  wl_status_t status = WiFi.status();

  if (status == WL_CONNECTED)
  {
    ConfigManager* configManager= ConfigManager::getInstance();

    configManager->setIPAddress(WiFi.localIP());
    configManager->setGateway(WiFi.subnetMask());
    configManager->setSubnet(WiFi.gatewayIP());

    Serial.printf("Connected to %s\n", configManager->getSSID());
    Serial.printf("IPAddress: %s\n", WiFi.localIP().toString().c_str());
    Serial.printf("Subnet Mask: %s\n", WiFi.subnetMask().toString().c_str());
    Serial.printf("Gateway IP: %s\n", WiFi.gatewayIP().toString().c_str());

    nextStep= eNetworkSettingsStep::Connected;
  }
  else
  {
    m_connectionTimeout-= deltaSeconds;
    if (m_connectionTimeout <= 0)
    {
      Serial.printf("Connect failed with status: %s", NetworkManager::WlStatusToStr(status));
      nextStep= eNetworkSettingsStep::FailedConnect;
    }
    else
    {
      Serial.print(".");
    }     
  }

  return nextStep;
}

void AppStage_NetworkSettings::render()
{
  //Serial.println("AppStage_NetworkSettings::render called");
  Adafruit_SSD1306* display= getApp()->getDisplay();

  display->clearDisplay();
  display->setTextSize(1);

  switch(m_step)
  {
  case eNetworkSettingsStep::ScanSSID:
    {
      display->setCursor(4,4);
      display->print("SSID Scan...");      
    }
    break;
  case eNetworkSettingsStep::SelectSSID:
  case eNetworkSettingsStep::FailedScanSSID:
    m_activeMenu->render();
    break;
  case eNetworkSettingsStep::VerifySSID:
    {
      ConfigManager* configManager= ConfigManager::getInstance();

      display->setCursor(28,4);
      if (configManager->hasSSID())
      {
        const char* ssid= configManager->getSSID();
        display->print(ssid);
      }
      else
      {
        display->print("<none>");
      }

      m_activeMenu->render();      
    }  
    break;
  case eNetworkSettingsStep::VerifyPassword:
    {
      ConfigManager* configManager= ConfigManager::getInstance();

      display->setCursor(28,4);
      if (configManager->hasPassword())
      {
        const char* password= configManager->getPassword();
        display->print(password);
      }
      else
      {
        display->print("<none>");
      }

      m_activeMenu->render();      
    }  
    break;
  case eNetworkSettingsStep::WaitForPassword:
    {
      display->setCursor(28,4);
      display->print("Set Password...");
    }  
    break;
  case eNetworkSettingsStep::Connecting:
    {
      ConfigManager* configManager= ConfigManager::getInstance();
      const char* ssid= configManager->getSSID();

      display->setCursor(4,4);
      display->print("Connecting to:");
      display->setCursor(4,14);
      display->print(ssid);
    } break;
  case eNetworkSettingsStep::Connected:
    {
      ConfigManager* configManager= ConfigManager::getInstance();

      display->setCursor(28,4);
      String ipAddress= configManager->getIPAddress().toString();
      display->print(ipAddress);

      m_activeMenu->render();      
    }  
    break;
  case eNetworkSettingsStep::FailedConnect:
    {
      m_activeMenu->render();
    }
    break;
  }

  display->display();
}