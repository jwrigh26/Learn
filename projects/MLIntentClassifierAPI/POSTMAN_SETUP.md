# ML Intent Classifier API - Postman Testing Guide

## Setup Instructions

1. Start the API:
   ```bash
   cd /Users/maneki-neko/learning/projects/MLIntentClassifierAPI
   dotnet run
   ```
   The API will run on `https://localhost:5001` (or the port shown in the console).

2. Import the Postman collection (see JSON below) or manually create requests.

## API Endpoints

### 1. Health Check
**GET** `https://localhost:5001/api/query/health`

**Purpose**: Check if the API is running and the ML model is loaded.

**Response Example**:
```json
{
  "status": "Healthy",
  "message": "Query Understanding Service is ready",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

### 2. Understand Query (Main Intent Classification)
**POST** `https://localhost:5001/api/query/understand`

**Purpose**: Analyze a user query to predict intent and extract slots.

**Request Headers**:
- Content-Type: `application/json`

**Request Body**:
```json
{
  "query": "Schedule a meeting with John tomorrow at 2pm"
}
```

**Response Example**:
```json
{
  "query": "Schedule a meeting with John tomorrow at 2pm",
  "predictedIntent": "ScheduleMeeting",
  "confidence": 0.85,
  "slots": {
    "employeeName": "John",
    "dateTime": "2024-01-16T14:00:00",
    "meetingType": null,
    "duration": null
  }
}
```

### 3. Test Queries (Batch Testing)
**GET** `https://localhost:5001/api/query/test-queries`

**Purpose**: Test the model with predefined sample queries.

**Response Example**:
```json
[
  {
    "query": "Schedule a meeting with Sarah next Monday",
    "predictedIntent": "ScheduleMeeting",
    "confidence": 0.92,
    "slots": {
      "employeeName": "Sarah",
      "dateTime": "2024-01-22T09:00:00",
      "meetingType": null,
      "duration": null
    }
  },
  {
    "query": "Cancel my 3pm appointment",
    "predictedIntent": "CancelMeeting",
    "confidence": 0.78,
    "slots": {
      "employeeName": null,
      "dateTime": "15:00:00",
      "meetingType": null,
      "duration": null
    }
  }
]
```

## Sample Test Queries

Try these queries in the `/understand` endpoint:

### Meeting Scheduling
- "Schedule a meeting with John tomorrow at 2pm"
- "Book a call with Sarah next Monday morning"
- "Set up a 1-hour meeting with the team on Friday"

### Meeting Cancellation
- "Cancel my meeting with John tomorrow"
- "Remove the 3pm appointment"
- "Delete the call scheduled for next week"

### Meeting Updates
- "Move my meeting with Sarah to 4pm"
- "Reschedule the team call to next Tuesday"
- "Change my appointment time to 10am"

### Information Requests
- "When is my next meeting?"
- "What meetings do I have tomorrow?"
- "Show me my schedule for next week"

## Postman Collection JSON

Create a new collection in Postman and import this JSON:

```json
{
  "info": {
    "name": "ML Intent Classifier API",
    "description": "API for intent classification and slot extraction",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Health Check",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/query/health",
          "host": ["{{baseUrl}}"],
          "path": ["api", "query", "health"]
        }
      }
    },
    {
      "name": "Understand Query",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"query\": \"Schedule a meeting with John tomorrow at 2pm\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/query/understand",
          "host": ["{{baseUrl}}"],
          "path": ["api", "query", "understand"]
        }
      }
    },
    {
      "name": "Test Queries",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/query/test-queries",
          "host": ["{{baseUrl}}"],
          "path": ["api", "query", "test-queries"]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:5001",
      "type": "string"
    }
  ]
}
```

## Environment Variables

In Postman, create an environment with:
- Variable: `baseUrl`
- Value: `https://localhost:5001` (or your actual API URL)

## Troubleshooting

### Common Issues:

1. **SSL Certificate Errors**: 
   - In Postman, go to Settings > General > SSL certificate verification and turn it OFF for local testing.

2. **Connection Refused**:
   - Make sure the API is running (`dotnet run`)
   - Check the correct port number in the console output

3. **Model Loading Errors**:
   - Verify the model file exists at `/Users/maneki-neko/learning/notebooks/NLP/intent_model.zip`
   - Check the console for detailed error messages

4. **500 Internal Server Error**:
   - Check the API console for detailed error logs
   - Ensure all required packages are installed

### Expected Model Performance:
- **High Confidence (>0.8)**: Clear, well-formed queries
- **Medium Confidence (0.5-0.8)**: Ambiguous or partial queries
- **Low Confidence (<0.5)**: Unclear or out-of-domain queries

The model should handle variations in language and extract key information like employee names, dates, and times from natural language input.
