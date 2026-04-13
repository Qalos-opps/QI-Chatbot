# QI — Qalos Intelligence
 Cybersecurity Awareness Bot | POE Part 1 | C# Console Application

A console-based chatbot built in C# that helps users learn about cybersecurity topics through interactive conversation.

 What It Does

When the application starts it will:
- Play a voice greeting (`greeting.wav`)
- Display the QI ASCII art logo
- Ask for your name and personalise the session
- Enter a chat loop where you can ask cybersecurity questions



 Topics the Bot Covers

| Topic                     | What You Will Learn |

| `password safety`         | Creating strong passwords, password managers, 2FA |
| `phishing`                | How to spot and avoid phishing emails and scams |
| `safe browsing`           | HTTPS, ad blockers, incognito mode, browser safety |
| `privacy`                 | Protecting personal data, VPNs, POPIA (South Africa) |
| `malware`                 | Viruses, ransomware, spyware, and how to prevent them |



 How to Run

1. Open Visual Studio and create a new **Console App (.NET Framework)** project
2. Replace `Program.cs` with the file from this repo
3. Add `greeting.wav` to the project folder and set **Copy to Output Directory** → **Copy Always**
4. Press **F5** to run

> **Note:** Audio playback uses `System.Media.SoundPlayer` which requires Windows. If the WAV file is missing the bot will show a text greeting and continue normally.

 

  Project Structure

All code is contained in a single `Program.cs` file, organised into 6 classes:

| Class                        | Role |
                     
| `Program`                    | Entry point — plays audio, shows logo, starts the bot |
| `QIBot`                      | Main controller — manages greeting and conversation loop |
| `UserInterface`              | All console output, colours, and formatting |
| `ResponseEngine`             | Keyword matching and cybersecurity responses |
| `InputValidator`             | Validates all user input before processing |
| `ValidationResult`           | Data container for validation results |



 Commands

```
password safety    → Password creation and management tips
phishing           → Phishing detection and avoidance
safe browsing      → Safe web browsing practices
privacy            → Personal data and privacy protection
malware            → Malware types and prevention
how are you        → Check QI status
what is your purpose → Learn about QI
what can i ask you → See all available topics
help               → Show all commands
clear              → Clear the screen
exit / quit / bye  → Quit the application
```



 Input Validation

The chatbot handles unexpected input gracefully and will never crash:
- Empty or whitespace input is rejected with a friendly message
- Input over 500 characters is rejected
- Invalid name entries prompt the user to try again
- Unrecognised questions return a helpful fallback message


Built With

- C# (.NET Framework 4.7.2+)
- `System.Media.SoundPlayer` for audio playback
- `System.Collections.Generic` for Dictionary data structures

Here is the YouTube Link : https://youtu.be/l7jnj5S8cqA


 CI/CD

*POE Part 1 — Cybersecurity Awareness Bot*
