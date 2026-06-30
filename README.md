# sn-mapper
Security Notes
What This Tool Does
✅ Reads memory of the binary using a debugger

✅ Extracts the IP→SN mapping

✅ Does NOT modify the binary or system

What This Tool Does NOT Do
❌ Does NOT crack or bypass authentication

❌ Does NOT modify the binary

❌ Does NOT exploit any vulnerabilities

❌ Does NOT connect to any remote systems

Important Security Considerations
The binary contains hardcoded credentials (password visible in decompiled code)

Host key verification is disabled (InsecureIgnoreHostKey)

This tool is for educational/research purposes only

Use only on binaries you own or have permission to analyze

🤝 Contributing
Contributions are welcome! Here's how to help:

Ways to Contribute
Bug Reports - Open an issue with details

Feature Requests - Suggest new features

Pull Requests - Submit improvements

Documentation - Help improve this README

Development Guidelines
Keep it simple - Bash scripts should be easy to understand

Add comments - Explain non-obvious code

Test thoroughly - Ensure it works on different systems

Update README - Document any new features

Reporting Issues
When opening an issue, please include:

Your OS and version (uname -a)

Delve version (dlv version)

The exact command you ran

The full error output

Steps to reproduce

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
