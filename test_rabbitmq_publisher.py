import pika
import json
import base64
import requests
from datetime import datetime
import uuid

# RabbitMQ bağlantısı
connection = pika.BlockingConnection(
    pika.URLParameters('amqp://dev:devpass@localhost:5672/')
)
channel = connection.channel()

# Queue'yu declare et
queue_name = 'plant-analysis-results'
channel.queue_declare(queue=queue_name, durable=True)

# Mock response'u oku
with open('test_mock_response.json', 'r', encoding='utf-8') as f:
    mock_response = json.load(f)

# Test with FreeImage.host integration - Add image URL field for testing
# Note: This simulates N8N returning an image URL instead of base64
# The image URL would be from our file storage service (FreeImage.host for development)
mock_response['image_url'] = 'https://iili.io/test-image/plant_analysis_test.jpg'
mock_response['image_metadata'] = {
    'format': 'JPEG',
    'size_bytes': 102400,  # 100KB after optimization
    'size_kb': 100,
    'size_mb': 0.1,
    'upload_timestamp': datetime.utcnow().isoformat() + 'Z',
    'storage_provider': 'FreeImageHost',
    'original_size_kb': 2048,  # 2MB original
    'compression_ratio': 95,  # 95% reduction
    'max_file_size': '64MB'  # FreeImage.host advantage over ImgBB (32MB)
}

# Update token usage to reflect URL method benefits
if 'token_usage' in mock_response and 'summary' in mock_response['token_usage']:
    mock_response['token_usage']['summary']['total_tokens'] = 1500  # Dramatically reduced from ~400K
    mock_response['token_usage']['summary']['total_cost_usd'] = '$0.0015'
    mock_response['token_usage']['summary']['total_cost_try'] = '₺0.04'
    mock_response['token_usage']['summary']['image_size_kb'] = 100  # Post-optimization size
    
    # Update token breakdown
    if 'token_breakdown' in mock_response['token_usage']:
        mock_response['token_usage']['token_breakdown']['input']['image'] = 765  # Much smaller
        mock_response['token_usage']['token_breakdown']['input']['total'] = 1000
        mock_response['token_usage']['token_breakdown']['grand_total'] = 1500

# Message properties
properties = pika.BasicProperties(
    correlation_id='test_corr_123',
    delivery_mode=2,  # Persistent
    content_type='application/json'
)

# Mesajı gönder
channel.basic_publish(
    exchange='',
    routing_key=queue_name,
    body=json.dumps(mock_response),
    properties=properties
)

print(f"Mock response sent to queue: {queue_name}")
print(f"Analysis ID: {mock_response['AnalysisId']}")
print(f"User ID: {mock_response['UserId']}")
print(f"Correlation ID: test_corr_123")
print(f"Image URL: {mock_response.get('image_url', 'N/A')}")
print(f"Storage Provider: {mock_response['image_metadata']['storage_provider']}")
print(f"Max File Size: {mock_response['image_metadata']['max_file_size']}")
print(f"Token Count: {mock_response['token_usage']['summary']['total_tokens']} (99.6% reduction)")
print(f"Cost: {mock_response['token_usage']['summary']['total_cost_usd']} (99.9% reduction)")

connection.close()