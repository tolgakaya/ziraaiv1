#!/usr/bin/env python3
"""
Convert Swagger/OpenAPI JSON to Postman Collection v2.1
Fixes:
- Proper variable syntax {{variable}} not {{{variable}}}
- Auto token management with pre-request scripts
- Comprehensive endpoint coverage
"""

import json
import uuid
from pathlib import Path
from typing import Dict, List, Any

def generate_postman_collection(swagger_path: str, output_path: str):
    """Convert swagger.json to Postman Collection v2.1"""

    # Read swagger
    with open(swagger_path, 'r', encoding='utf-8') as f:
        swagger = json.load(f)

    # Initialize Postman collection
    collection = {
        "info": {
            "_postman_id": str(uuid.uuid4()),
            "name": "ZiraAI API - Complete Collection",
            "description": "Auto-generated from Swagger - All endpoints with proper auth",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
        },
        "item": [],
        "auth": {
            "type": "bearer",
            "bearer": [
                {
                    "key": "token",
                    "value": "{{access_token}}",
                    "type": "string"
                }
            ]
        },
        "event": [
            {
                "listen": "prerequest",
                "script": {
                    "type": "text/javascript",
                    "exec": [
                        "// Auto-refresh token if expired",
                        "const tokenExpiry = pm.collectionVariables.get('token_expiry');",
                        "const now = new Date().getTime();",
                        "",
                        "if (!tokenExpiry || now >= tokenExpiry) {",
                        "    console.log('Token expired or missing, please login first');",
                        "}"
                    ]
                }
            },
            {
                "listen": "test",
                "script": {
                    "type": "text/javascript",
                    "exec": [
                        "// Auto-extract token from login/register responses",
                        "if (pm.response.code === 200 && pm.info.requestName.includes('Login')) {",
                        "    const response = pm.response.json();",
                        "    if (response.data && response.data.token) {",
                        "        pm.collectionVariables.set('access_token', response.data.token);",
                        "        pm.collectionVariables.set('refresh_token', response.data.refreshToken);",
                        "        ",
                        "        // Calculate expiry (1 hour from now)",
                        "        const expiry = new Date().getTime() + (60 * 60 * 1000);",
                        "        pm.collectionVariables.set('token_expiry', expiry);",
                        "        ",
                        "        console.log('[OK] Token auto-saved');",
                        "    }",
                        "}"
                    ]
                }
            }
        ],
        "variable": [
            {
                "key": "base_url",
                "value": "https://localhost:5001",
                "type": "string"
            },
            {
                "key": "version",
                "value": "1",
                "type": "string"
            },
            {
                "key": "access_token",
                "value": "",
                "type": "string"
            },
            {
                "key": "refresh_token",
                "value": "",
                "type": "string"
            },
            {
                "key": "token_expiry",
                "value": "",
                "type": "string"
            }
        ]
    }

    # Group endpoints by tag
    folders: Dict[str, List[Dict]] = {}

    for path, methods in swagger.get('paths', {}).items():
        for method, details in methods.items():
            if method.upper() not in ['GET', 'POST', 'PUT', 'DELETE', 'PATCH']:
                continue

            # Get tag (folder name)
            tags = details.get('tags', ['Other'])
            tag = tags[0] if tags else 'Other'

            # Create folder if not exists
            if tag not in folders:
                folders[tag] = []

            # Build request
            request = build_postman_request(path, method.upper(), details)
            folders[tag].append(request)

    # Convert folders to Postman format
    for folder_name in sorted(folders.keys()):
        collection['item'].append({
            "name": folder_name,
            "item": folders[folder_name],
            "description": f"{folder_name} endpoints"
        })

    # Write output
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(collection, f, indent=2, ensure_ascii=False)

    print(f"[OK] Postman collection created: {output_path}")
    print(f"Total folders: {len(folders)}")
    print(f"Total requests: {sum(len(items) for items in folders.values())}")

def build_postman_request(path: str, method: str, details: Dict) -> Dict:
    """Build a Postman request item"""

    # Convert swagger path params to Postman variables
    # /api/v{version}/Auth/login -> /api/v{{version}}/Auth/login
    postman_path = path.replace('{version}', '{{version}}')
    for param in ['id', 'code', 'userId', 'analysisId']:
        postman_path = postman_path.replace('{' + param + '}', '{{' + param + '}}')

    # Build URL
    url = {
        "raw": "{{base_url}}" + postman_path,
        "host": ["{{base_url}}"],
        "path": [p for p in postman_path.strip('/').split('/') if p]
    }

    # Build body
    body = {}
    request_body = details.get('requestBody', {})
    if request_body:
        content = request_body.get('content', {})
        json_schema = content.get('application/json', {}).get('schema', {})

        if json_schema:
            # Get example body from schema
            body = {
                "mode": "raw",
                "raw": json.dumps(get_example_body(json_schema), indent=2),
                "options": {
                    "raw": {
                        "language": "json"
                    }
                }
            }

    # Build headers
    headers = [
        {
            "key": "Content-Type",
            "value": "application/json",
            "type": "text"
        }
    ]

    # Build auth (check if endpoint needs auth)
    auth = None
    security = details.get('security', [])
    if not security or any('Bearer' in s for s in security):
        # Use collection auth (bearer token)
        auth = {
            "type": "bearer",
            "bearer": [
                {
                    "key": "token",
                    "value": "{{access_token}}",
                    "type": "string"
                }
            ]
        }

    # Build request name
    operation_id = details.get('operationId', '')
    summary = details.get('summary', '')
    name = summary or operation_id or f"{method} {path}"

    request = {
        "name": name,
        "request": {
            "method": method,
            "header": headers,
            "url": url
        },
        "response": []
    }

    if body:
        request['request']['body'] = body

    if auth and 'login' not in name.lower() and 'register' not in name.lower():
        request['request']['auth'] = auth

    # Add description
    description = details.get('description', '')
    if description:
        request['request']['description'] = description

    return request

def get_example_body(schema: Dict) -> Dict:
    """Generate example request body from schema"""

    # Try to get $ref
    if '$ref' in schema:
        # Can't resolve refs without full schema, return placeholder
        return {"placeholder": "Fill with actual data"}

    # Get properties
    properties = schema.get('properties', {})
    example = {}

    for prop_name, prop_schema in properties.items():
        prop_type = prop_schema.get('type', 'string')

        if prop_type == 'string':
            example[prop_name] = prop_schema.get('example', f"example_{prop_name}")
        elif prop_type == 'integer':
            example[prop_name] = prop_schema.get('example', 0)
        elif prop_type == 'boolean':
            example[prop_name] = prop_schema.get('example', False)
        elif prop_type == 'array':
            example[prop_name] = []
        elif prop_type == 'object':
            example[prop_name] = {}

    return example if example else {"placeholder": "Fill with actual data"}

if __name__ == '__main__':
    swagger_path = Path(__file__).parent / 'swagger.json'
    output_path = Path(__file__).parent / 'ZiraAI_Complete_Collection.postman_collection.json'

    generate_postman_collection(str(swagger_path), str(output_path))
