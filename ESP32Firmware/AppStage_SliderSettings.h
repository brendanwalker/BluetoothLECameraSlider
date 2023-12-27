#ifndef AppStage_SliderSettings_h
#define AppStage_SliderSettings_h

#include "AppStage.h"
#include "ConfigManager.h"
#include "SelectionMenu.h"

class AppStage_SliderSettings : public AppStage, public SelectionMenuListener
{
public:
  AppStage_SliderSettings(class App* app);

  static AppStage_SliderSettings* getInstance() { return s_instance; }

  virtual void enter() override;
  virtual void exit() override;
  virtual void render() override;

  // Selection Events
  virtual void onOptionClicked(int optionIndex) override;  

  static const char* APP_STAGE_NAME;	

private:
  static AppStage_SliderSettings* s_instance;

  SelectionMenu m_selectionMenu;
};

#endif
