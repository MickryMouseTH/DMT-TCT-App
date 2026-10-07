FROM python:3.12-slim
RUN pip install --no-cache-dir aiohttp==3.10.10
WORKDIR /app
COPY lcsd_sim.py panel.html keyboard.html edc.html ./
EXPOSE 58084 8090
CMD ["python", "-u", "lcsd_sim.py"]
