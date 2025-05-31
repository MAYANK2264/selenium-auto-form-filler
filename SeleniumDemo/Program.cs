using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static IWebDriver driver;
    private static WebDriverWait wait;
    private const int WAIT_TIMEOUT = 10;
    private const string TEST_URL = "https://app.cloudqa.io/home/AutomationPracticeForm";

    static void TakeScreenshot(string fileName)
    {
        try
        {
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(fileName);
            Console.WriteLine($"Screenshot saved as {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to take screenshot: {ex.Message}");
        }
    }

    static void InitializeDriver()
    {
        Console.WriteLine("Initializing WebDriver...");
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        driver = new ChromeDriver(service, options);
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(WAIT_TIMEOUT));
        Console.WriteLine("✓ WebDriver initialized successfully\n");
    }

    static void RunTests()
    {
        NavigateToForm();
        
        // Test all form fields
        FillMainFormSection();
        SwitchToAndFillIframeSection();
        FillShadowDOMSection();

        Console.WriteLine("\n✓ All form sections completed successfully!");
    }

    static void NavigateToForm()
    {
        Console.WriteLine($"Navigating to {TEST_URL}");
        driver.Navigate().GoToUrl(TEST_URL);
        WaitForElement(By.Id("fname"), "First Name field");
        Console.WriteLine("✓ Successfully navigated to form page\n");
    }

    static void FillMainFormSection()
    {
        Console.WriteLine("Filling main form section...");

        // 1. First Name
        FillTextField(new List<By> {
            By.Id("fname"),
            By.Name("firstname"),
            By.CssSelector("input[placeholder*='first name' i]")
        }, "MAYANK", "First Name");

        // 2. Last Name
        FillTextField(new List<By> {
            By.Id("lname"),
            By.Name("lastname"),
            By.CssSelector("input[placeholder*='last name' i]")
        }, "CHOUHAN", "Last Name");

        // 3. Gender
        SelectRadioButton(new List<By> {
            By.XPath("//input[@type='radio' and (@value='Male' or @id='male')]"),
            By.XPath("//label[normalize-space(text())='Male']/input[@type='radio']")
        }, "Male");

        // 4. Email
        FillTextField(new List<By> {
            By.Id("email"),
            By.Name("email"),
            By.CssSelector("input[type='email']")
        }, "kmmayank08@gmail.com", "Email");

        Console.WriteLine("✓ Main form section completed\n");
    }

    static void SwitchToAndFillIframeSection()
    {
        Console.WriteLine("Switching to iframe section...");
        
        try
        {
            // Find iframe using multiple strategies
            var iframes = driver.FindElements(By.TagName("iframe"));
            bool iframeFound = false;

            foreach (var iframe in iframes)
            {
                try
                {
                    driver.SwitchTo().Frame(iframe);
                    
                    // Try to find a known element to verify we're in the correct iframe
                    var elements = driver.FindElements(By.CssSelector("input[type='checkbox']"));
                    if (elements.Any())
                    {
                        iframeFound = true;
                        Console.WriteLine("Found correct iframe, filling hobbies section...");

                        // 5. Hobbies
                        SelectCheckbox(new List<By> {
                            By.XPath("//input[@type='checkbox' and (@value='Cricket' or @id='cricket')]"),
                            By.XPath("//label[contains(text(), 'Cricket')]/input[@type='checkbox']")
                        }, "Cricket");

                        break;
                    }
                    
                    driver.SwitchTo().ParentFrame();
                }
                catch (Exception)
                {
                    driver.SwitchTo().DefaultContent();
                    continue;
                }
            }

            if (!iframeFound)
            {
                throw new Exception("Could not find the iframe containing hobbies section");
            }

            // Switch back to main content
            driver.SwitchTo().DefaultContent();
            Console.WriteLine("✓ Iframe section completed\n");
        }
        catch (Exception ex)
        {
            driver.SwitchTo().DefaultContent();
            throw new Exception($"Error in iframe section: {ex.Message}", ex);
        }
    }

    static void FillShadowDOMSection()
    {
        Console.WriteLine("Accessing shadow DOM section...");

        try
        {
            // 6. About Yourself
            // First find the shadow host
            IWebElement shadowHost = FindElement(new List<By> {
                By.CssSelector("[data-shadow-host]"),
                By.CssSelector("[class*='shadow']"),
                By.Id("shadow-host")
            }, "Shadow DOM host");

            // Access shadow root using JavaScript
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            var shadowRoot = js.ExecuteScript("return arguments[0].shadowRoot", shadowHost);

            // Find and fill the textarea within shadow DOM
            if (shadowRoot != null)
            {
                js.ExecuteScript(@"
                    const textarea = arguments[0].shadowRoot.querySelector('textarea');
                    if (textarea) {
                        textarea.value = arguments[1];
                        textarea.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                ", shadowHost, "i am a coder a devloper and a leader");

                Console.WriteLine("✓ Shadow DOM section completed\n");
            }
            else
            {
                throw new Exception("Could not access shadow root");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error in shadow DOM section: {ex.Message}", ex);
        }
    }

    static void FillTextField(List<By> locators, string value, string fieldName)
    {
        Console.WriteLine($"Filling {fieldName} field with: {value}");
        
        IWebElement element = FindElement(locators, fieldName);
        ScrollToElement(element);
        
        element.Clear();
        element.SendKeys(value);
        
        // Verify input
        string enteredValue = element.GetAttribute("value");
        if (enteredValue != value)
        {
            throw new Exception($"{fieldName} verification failed. Expected: '{value}', got: '{enteredValue}'");
        }
    }

    static void SelectRadioButton(List<By> locators, string value)
    {
        Console.WriteLine($"Selecting radio button: {value}");
        
        IWebElement element = FindElement(locators, $"{value} radio button");
        ScrollToElement(element);
        
        if (!element.Selected)
        {
            element.Click();
            System.Threading.Thread.Sleep(500);
        }

        if (!element.Selected)
        {
            throw new Exception($"Failed to select {value} radio button");
        }
    }

    static void SelectCheckbox(List<By> locators, string value)
    {
        Console.WriteLine($"Selecting checkbox: {value}");
        
        IWebElement element = FindElement(locators, $"{value} checkbox");
        ScrollToElement(element);
        
        if (!element.Selected)
        {
            element.Click();
            System.Threading.Thread.Sleep(500);
        }

        if (!element.Selected)
        {
            throw new Exception($"Failed to select {value} checkbox");
        }
    }

    static IWebElement FindElement(List<By> locators, string elementName)
    {
        foreach (var locator in locators)
        {
            try
            {
                return wait.Until(d => d.FindElement(locator));
            }
            catch (WebDriverTimeoutException)
            {
                continue;
            }
        }
        throw new NoSuchElementException($"Could not find {elementName} using any of the provided locators");
    }

    static void WaitForElement(By locator, string elementName)
    {
        try
        {
            wait.Until(d => d.FindElement(locator));
        }
        catch (WebDriverTimeoutException)
        {
            throw new Exception($"Timeout waiting for {elementName}");
        }
    }

    static void ScrollToElement(IWebElement element)
    {
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            System.Threading.Thread.Sleep(300);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to scroll to element: {ex.Message}");
        }
    }

    static void CleanUp()
    {
        if (driver != null)
        {
            Console.WriteLine("\nClosing browser...");
            driver.Quit();
        }
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Starting Form Automation Test...\n");
        try
        {
            InitializeDriver();
            RunTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed!");
            Console.WriteLine($"Error: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            CleanUp();
        }
    }
} 