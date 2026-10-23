FROM ubuntu:24.04 AS build
RUN apt-get update && apt-get install -y --no-install-recommends build-essential cmake libevent-dev && rm -rf /var/lib/apt/lists/*
COPY ["Tools/YGO Classes", "/src/Tools/YGO Classes"]
COPY Server/CMakeLists.txt /src/Server/CMakeLists.txt
RUN cmake -S /src/Server -B /build -DCMAKE_BUILD_TYPE=Release && cmake --build /build -j2

FROM ubuntu:24.04
RUN apt-get update && apt-get install -y --no-install-recommends python3 openssl ca-certificates libevent-2.1-7t64 libevent-pthreads-2.1-7t64 && rm -rf /var/lib/apt/lists/*
RUN useradd --uid 1001 --create-home ultra
WORKDIR /app
COPY --from=build /build/libocgcore.so /build/libygoserver.so /app/native/
COPY Data/locales/en-US/cards.cdb /app/cards.cdb
COPY Data/script.zip /app/data/script.zip
COPY Data/lflist.conf Data/lflist_genesys.conf /app/Data/
COPY Server/service.py /app/service.py
RUN mkdir /app/Expansions /app/Replay /app/config && chown -R ultra:ultra /app
USER ultra
ENV ULTRA_STATE=/state ULTRA_HOST=69.48.201.156 ULTRA_PORT=9444
CMD ["python3", "-u", "service.py"]
