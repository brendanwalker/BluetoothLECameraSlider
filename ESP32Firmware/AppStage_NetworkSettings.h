#ifndef AppStage_NetworkSettings_h
#define AppStage_NetworkSettings_h

#include "AppStage.h"
#include "ConfigManager.h"
#include "SelectionMenu.h"

enum class eNetworkSettingsStep : int
{
  INVALID= -1,

  ScanSSID,
  VerifySSID,
  SelectSSID,
  FailedScanSSID,
  VerifyPassword,
  WaitForPassword,
  Connecting,
  Connected,  
  FailedConnect,

  COUNT
};

class AppStage_NetworkSettings : public AppStage, public ConfigEventListener, public SelectionMenuListener
{
public:
	AppStage_NetworkSettings(class App* app);

  static AppStage_NetworkSettings* getInstance() { return s_instance; }

  void setAutoExitOnConnect(bool bFlag) { m_bAutoExitOnConnect= bFlag; }

	virtual void enter() override;
  virtual void exit() override;
  virtual void update(float deltaSeconds) override;
  virtual void render() override;

  // Config Events
  virtual void onPasswordChanged() override;

  // Selection Events
  virtual void onOptionClicked(int optionIndex) override;  

	static const char* APP_STAGE_NAME;	

private:
  static AppStage_NetworkSettings* s_instance;

  void setState(eNetworkSettingsStep newState);
  void onLeaveStep(eNetworkSettingsStep oldState);
  void onEnterStep(eNetworkSettingsStep newState);

  eNetworkSettingsStep update_scanSSID(float deltaSeconds);
  eNetworkSettingsStep update_connecting(float deltaSeconds);

  int m_ssidCount= 0;
  String* m_ssidStrings= nullptr;
  eNetworkSettingsStep m_step;
  SelectionMenu* m_activeMenu= nullptr;
  float m_connectionTimeout= 0.f;
  bool m_bAutoExitOnConnect= false;
};

#endif
