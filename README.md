<img width="1086" height="756" alt="Screenshot 2026-06-02 133722" src="" />
<img width="1090" height="762" alt="Screenshot 2026-06-02 134112" src="" />
<img width="846" height="733" alt="Screenshot 2026-06-02 134252" src="" />
<img width="964" height="839" alt="Screenshot 2026-06-02 134344" src="https://github.com/user-attachments/assets/29c8d947-0e4a-47dd-ba53-fb597504840e" />
# CurrentlyListening

**CurrentlyListening** is a free desktop utility that lets you export the currently playing song on Spotify to a simple text file, updated in real-time. Perfect for stream overlays, OBS integrations, or personal use.

![image](https://github.com/user-attachments/assets/7a807e06-1297-46a4-886d-e2795afe766c](https://github.com/user-attachments/assets/b1f98012-7269-4beb-8505-892f57aab1cd))
![image](https://github.com/user-attachments/assets/fcc2c885-6595-4c0f-a69a-1df847dba2ff](https://github.com/user-attachments/assets/b44738e7-cb9c-46f5-91be-e0cddb2df8d7))
![image](https://github.com/user-attachments/assets/5a955ab4-9ea8-441b-9dd9-905953fd26cf](https://github.com/user-attachments/assets/db0baaf4-e095-4d31-87ab-0464b9304bca))
![image]([https://github.com/user-attachments/assets/01f29401-1d77-418c-8bb7-f51c3201e1cd](https://github.com/user-attachments/assets/29c8d947-0e4a-47dd-ba53-fb597504840e))





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
