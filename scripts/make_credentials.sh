openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -sha256 -days 3650 -nodes -subj "/C=AU/O=RobotManager"
rm -r credentials
mkdir credentials
mv key.pem ./credentials
mv cert.pem ./credentials
