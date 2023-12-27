#ifndef AppStage_h
#define AppStage_h

#include <Arduino.h>

class AppStage
{
public:
    AppStage(
        class App *app,
        const String &stageName);
    virtual ~AppStage();

    inline class App* getApp() { return m_app; }

    // Transition Events
    virtual void enter() {}
    virtual void pause() {}
    virtual void resume() {}
    virtual void exit() {}

    // Loop Events
    virtual void update(float deltaSeconds) {}
    virtual void render() {}

protected:
    class App *m_app;
    bool m_bIsEntered = false;
    bool m_bIsPaused = false;
    String m_appStageName;
};

#endif