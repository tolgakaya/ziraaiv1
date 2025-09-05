# Ultra-minimal Railway test container
FROM alpine:latest

RUN echo "TEST: Container building..." && \
    echo "#!/bin/sh" > /test.sh && \
    echo "echo 'RAILWAY TEST: Container started successfully'" >> /test.sh && \
    echo "echo 'Time: \$(date)'" >> /test.sh && \
    echo "echo 'Environment: \$RAILWAY_ENVIRONMENT_NAME'" >> /test.sh && \
    echo "echo 'Port: \$PORT'" >> /test.sh && \
    echo "while true; do echo 'RAILWAY TEST: Still running...'; sleep 30; done" >> /test.sh && \
    chmod +x /test.sh

CMD ["/bin/sh", "/test.sh"]