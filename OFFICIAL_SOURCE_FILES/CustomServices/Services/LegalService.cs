namespace MiniGames.CustomServices.Services;

public class LegalService : ILegalService
{
    public string GetTermsOfService()
    {
        return @"**Terms of Service**

**1. Acceptance of Terms**
By accessing or using the MiniGames website and emulator services, you agree to be bound by these Terms.

**2. Description of Service**
MiniGames provides a web‑based emulation platform for playing retro games. All game ROMs are user‑provided; we do not host copyrighted content.

**3. User Responsibilities**
You are solely responsible for any ROMs you load. You must own the original game or have permission to use the ROM.

**4. Intellectual Property**
The emulator code is open‑source under the MIT License. Game titles, logos, and trademarks are property of their respective owners.

**5. Limitation of Liability**
The service is provided ""as is"" without warranties. We are not liable for any damages arising from your use.

**6. Termination**
We reserve the right to suspend or terminate access for violations of these terms.";
    }

    public string GetPrivacyPolicy()
    {
        return @"**Privacy Policy**

**Information Collection**
We do not collect personally identifiable information unless you voluntarily submit it (e.g., via error reports). We may collect anonymous usage data to improve the service.

**Cookies**
We use essential cookies for functionality (e.g., saving preferences). No tracking cookies are used.

**Third‑Party Services**
Our error reporting may send data to our servers; this is used solely for debugging.

**Data Security**
We take reasonable measures to protect any data you submit, but cannot guarantee absolute security.

**Changes to This Policy**
We may update this policy; continued use constitutes acceptance.

**Contact**
For privacy concerns, email privacy@minigames.example.";
    }
}