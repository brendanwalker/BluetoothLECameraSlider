#include "App.h"
#include "AppStage.h"

AppStage::AppStage(
    App *app,
    const String &stageName)
    : m_app(app), m_bIsEntered(false), m_bIsPaused(false), m_appStageName(stageName)
{
}

AppStage::~AppStage()
{
}