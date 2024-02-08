#include <Arduino.h>
#include "App.h"
#include "Button2.h"
#include "SelectionMenu.h"
#include "RotaryEncoder.h"
#include <Adafruit_SSD1306.h>

#define MAX_DISPLAY_LINES   5

SelectionMenu::SelectionMenu(const String& title, const String* options, int optionCount)
  : m_title(title)
  , m_options(options)
  , m_optionCount(optionCount)
  , m_startYPos(4)
{
}

void SelectionMenu::addHeader(const String& header)
{
  if (m_headerCount < MAX_HEADER_COUNT)
  {
    m_headers[m_headerCount]= header;
    m_headerCount++;
  }
}

void SelectionMenu::show()
{
  App* app= App::getInstance();
  RotaryHalfStep* rotaryEncoder= app->getRotaryEncoder();

  app->pushInputListener(this);

  m_lineStartIndex= 0;
  m_lineIndex= 0;
  m_lineEndIndex= min(getLineCount() - 1, MAX_DISPLAY_LINES - 1);
}

void SelectionMenu::hide()
{
  App* app= App::getInstance();
  app->popInputListener();
}

void SelectionMenu::render()
{
  App* app= App::getInstance();
  Adafruit_SSD1306* display= app->getDisplay();

  display->clearDisplay();
  display->setTextSize(1);

  int yPos= m_startYPos;
  display->setCursor(2, m_startYPos);
  if (m_title.length() > 0)
  {
    display->print(m_title);
    yPos+= m_lineHeight;
  }

  if (m_headerCount > 0 || m_optionCount > 0)
  {
    for (int lineIndex= m_lineStartIndex; lineIndex <= m_lineEndIndex; lineIndex++)
    {
      if (lineIndex == m_lineIndex)
      {
        display->setCursor(4, yPos);
        display->print(isHeaderLine(lineIndex) ? F("-") : F(">"));
      }

      display->setCursor(12, yPos);
      const String* line= getLine(lineIndex);
      if (line != nullptr)
        display->print(*line);
      else
        display->print("<null>");

      yPos+= m_lineHeight;
    }
  }
  else
  {
    display->print("<Empty>");
  }

  display->display();
}

// Input Events
void SelectionMenu::onRotaryEncoderValueChanged(RotaryHalfStep* rotaryEncoder)
{
		m_lineIndex= rotaryEncoder->getPosition();
    //Serial.printf("line index change in %s to %d\n", m_title, m_lineIndex);

    if (m_lineIndex < m_lineStartIndex)
    {
      m_lineStartIndex= m_lineIndex;
      m_lineEndIndex= min(m_lineStartIndex + MAX_DISPLAY_LINES - 1, getLineCount() - 1);
    }
    else if (m_lineIndex > m_lineEndIndex)
    {
      m_lineEndIndex= m_lineIndex;
      m_lineStartIndex= max(m_lineEndIndex - MAX_DISPLAY_LINES + 1, 0);
    }

    if (m_menuListener != nullptr)
    {
      int optionIndex= -1;
      if (getOptionIndex(m_lineIndex, optionIndex))
      {
        m_menuListener->onOptionChanged(optionIndex);
      }
    }
}

void SelectionMenu::onRotaryButtonClicked(Button2* button)
{
  if (m_menuListener != nullptr)
  {
      int optionIndex= -1;
      if (getOptionIndex(m_lineIndex, optionIndex))
      {
        m_menuListener->onOptionClicked(optionIndex);
      }
  }
}

int SelectionMenu::getLineCount() const
{
  return m_headerCount + m_optionCount;
}

bool SelectionMenu::isHeaderLine(int lineIndex) const
{
  return lineIndex >= 0 && lineIndex < m_headerCount;
}

bool SelectionMenu::getOptionIndex(int lineIndex, int& optionIndex) const
{
  optionIndex= m_lineIndex - m_headerCount;
  return optionIndex >= 0 && optionIndex < m_optionCount;
}

const String* SelectionMenu::getLine(int lineIndex)
{
  if (isHeaderLine(lineIndex))
  {
    return &m_headers[lineIndex];
  }
  else
  {
    int optionIndex = lineIndex - m_headerCount;

    if (optionIndex >= 0 && optionIndex < m_optionCount)
    {
      return &m_options[optionIndex];
    }
  }

  return nullptr;
}
