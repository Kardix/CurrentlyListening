# CurrentlyListening

**CurrentlyListening** is a free desktop utility that lets you export the currently playing song on Spotify to a simple text file, updated in real-time. Perfect for stream overlays, OBS integrations, or personal use.

![image](https://github.com/user-attachments/assets/7a807e06-1297-46a4-886d-e2795afe766c)
![image](https://github.com/user-attachments/assets/fcc2c885-6595-4c0f-a69a-1df847dba2ff)
![image](https://github.com/user-attachments/assets/5a955ab4-9ea8-441b-9dd9-905953fd26cf)
![image](https://github.com/user-attachments/assets/01f29401-1d77-418c-8bb7-f51c3201e1cd)





## ✨ Features

- Real-time display of the current Spotify track.
- Customizable output format: choose to display artist, song title, and/or time (progress/duration).
- Support for multiple languages: English, French, Spanish, German, Czech, Portuguese, Ukrainian, and Polish.
- Option to use custom Spotify Client ID and Secret for authentication.
- Output preview to see how your display will look.
- Ability to set the output file location for integration with streaming software.
- Lightweight and user-friendly interface.
- Free to use under a non-commercial, no-derivatives license

## 🚀 Getting Started

### Prerequisites

- Windows 10 or later.
- A Spotify account.
- .NET 8 Desktop Runtime
- Spotify Premium account (required for API access)

### Installation

1. Download the latest release from the [Releases](https://github.com/Kardix/CurrentlyListening/releases) page.
2. Run the installer.
3. Done.
   > The app is not signed yet, so you may see a Windows Defender SmartScreen warning — click *More info* → *Run anyway*.


### Spotify Authentication

To allow the application to access your Spotify account:

1. Click on the **"Login to Spotify"** button.
2. A browser window will open prompting you to log in to your Spotify account and authorize the application.
3. After authorization, the application will be connected to your Spotify account.

### Using Custom Spotify Credentials (Optional)

If you prefer to use your own Spotify Client ID and Secret:

1. Visit the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard).
2. Log in and create a new application.
3. Set the Redirect URI to `http://127.0.0.1:5000/callback`.
4. Copy your Client ID and Client Secret.
5. In the application, check the **"Use custom Client ID and Secret"** option.
6. Enter your Client ID and Secret in the provided fields.

## 🌐 Localization

CurrentlyListening supports multiple languages. You can select your preferred language within the application settings.

**Supported languages:**

- English
- French
- Spanish
- German
- Czech
- Portuguese
- Ukrainian
- Polish


## 🔐 Privacy

The app stores access tokens only locally and never sends your data to any third party. You control your own output path.

## 🧾 License

This project is licensed under the **Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License**.

You may:
- Use the app freely for personal/non-commercial use
- Share it with proper attribution

You may not:
- Modify or fork the app
- Sell it or bundle it with paid offerings

Author: **Tom "TKoNoR"**

## 📫 Contact

- Email: [tkonorgaming@gmail.com](mailto:tkonorgaming@gmail.com)

---

If you enjoy the app, consider sharing it with others!
