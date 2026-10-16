FROM python:3.12-slim
RUN apt-get update && apt-get install -y --no-install-recommends fonts-thai-tlwg fonts-dejavu-core \
 && rm -rf /var/lib/apt/lists/*
RUN pip install --no-cache-dir aiohttp==3.10.10 pillow==11.0.0
WORKDIR /app
COPY lcsd_sim.py panel.html keyboard.html edc.html tfi.html ./
EXPOSE 58084 8090
CMD ["python", "-u", "lcsd_sim.py"]
