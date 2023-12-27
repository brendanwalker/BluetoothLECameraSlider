#ifndef SelectionMenu_h
#define SelectionMenu_h

#include "Arduino.h"
#include "App.h"
#include <functional>

#define MAX_HEADER_COUNT  6

class SelectionMenuListener
{
public:
  virtual void onOptionChanged(int optionIndex) { }
  virtual void onOptionClicked(int optionIndex) { }
};

class SelectionMenu : public InputEventListener
{
public:
  SelectionMenu(const String& title, const String* options, int optionCount);

  void addHeader(const String& header);

  void show();
  void hide();
  void render();

  // Input Events
  virtual bool getIsRotaryEncoderWrapped() const override { return true; }
  virtual int getRotaryEncoderDefaultValue() const override { return 0; }
  virtual int getRotaryEncoderLowerBound() const override { return 0; }
  virtual int getRotaryEncoderUpperBound() const override { return getLineCount() - 1; }  
  virtual void onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder) override;
  virtual void onRotaryButtonClicked(class Button2* button) override;

  // Option Events
  inline void setListener(SelectionMenuListener* listener) { m_menuListener= listener; }
  inline void clearListener() { m_menuListener= nullptr; }

private:
  String m_title;

  int getLineCount() const;
  bool isHeaderLine(int lineIndex) const;  
  bool getOptionIndex(int lineIndex, int& optionIndex) const;
  const String* getLine(int lineIndex);

  String m_headers[MAX_HEADER_COUNT];
  int m_headerCount= 0;
  
  const String* m_options= nullptr;
  int m_optionCount= 0;
  
  int m_startYPos= 0;
  int m_lineHeight= 10;
  int m_lineStartIndex= 0;
  int m_lineIndex= 0;
  int m_lineEndIndex= 0;
  SelectionMenuListener* m_menuListener= nullptr;
};

#endif // SelectionMenu_h