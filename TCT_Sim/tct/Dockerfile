FROM mono:6.12

# Debian buster ถูกย้ายไป archive แล้ว
RUN sed -i "s|deb.debian.org|archive.debian.org|g; s|security.debian.org|archive.debian.org|g; /buster-updates/d" /etc/apt/sources.list \
 && apt-get -o Acquire::Check-Valid-Until=false update -qq \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends \
      gtk-sharp2 xvfb x11vnc novnc websockify openbox fonts-thai-tlwg fonts-dejavu-core procps x11-apps imagemagick xdotool python3 \
 && rm -rf /var/lib/apt/lists/*

COPY start.sh keyinject.py /
RUN chmod +x /start.sh
EXPOSE 6080 9094 9095
CMD ["/start.sh"]
