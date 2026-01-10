#!/usr/bin/env python3
"""
Weather Alert Producer for Confluent Cloud

Sends Avro-formatted weather alert messages to the weather-alerts topic
in Confluent Cloud using Schema Registry.

Usage:
    python produce-weather-alerts.py                    # Send all sample messages
    python produce-weather-alerts.py --count 1          # Send 1 random message
    python produce-weather-alerts.py --severity CRITICAL # Send only CRITICAL messages
    python produce-weather-alerts.py --interactive      # Interactive mode
"""

import argparse
import os
import sys
import time
from typing import Dict, Any

try:
    from confluent_kafka import SerializingProducer
    from confluent_kafka.serialization import StringSerializer
    from confluent_kafka.schema_registry import SchemaRegistryClient
    from confluent_kafka.schema_registry.avro import AvroSerializer
except ImportError:
    print("ERROR: confluent-kafka package not found!")
    print("Install it with: pip install confluent-kafka[avro]")
    sys.exit(1)


# Sample weather alert messages
SAMPLE_MESSAGES = [
    {
        "messageId": "test-critical-heat-001",
        "subject": "🔴 CRITICAL: Extreme Heat Emergency - Phoenix",
        "body": "",
        "recipient": "cnabilou@gmail.com",
        "timestamp": int(time.time() * 1000),
        "alertType": "TEMPERATURE_EXTREME",
        "severity": "CRITICAL",
        "location": {
            "zipCode": "85001",
            "city": "Phoenix",
            "state": "Arizona",
            "latitude": 33.4484,
            "longitude": -112.0740
        },
        "weatherConditions": {
            "currentTemperature": 48.0,
            "weatherCode": 0,
            "weatherDescription": "Clear sky - Extreme heat advisory",
            "windSpeed": 8.0,
            "precipitation": 0.0
        },
        "metadata": {
            "from": "emergency-alerts@weather.gov",
            "priority": "critical"
        }
    },
    {
        "messageId": "test-severe-storm-001",
        "subject": "🌩️ Severe Thunderstorm Warning - San Francisco",
        "body": "",
        "recipient": "cnabilou@gmail.com",
        "timestamp": int(time.time() * 1000),
        "alertType": "SEVERE_WEATHER",
        "severity": "SEVERE",
        "location": {
            "zipCode": "94105",
            "city": "San Francisco",
            "state": "California",
            "latitude": 37.7749,
            "longitude": -122.4194
        },
        "weatherConditions": {
            "currentTemperature": 15.5,
            "weatherCode": 95,
            "weatherDescription": "Thunderstorm with heavy hail",
            "windSpeed": 65.0,
            "precipitation": 35.2
        },
        "metadata": {
            "from": "storm-alerts@weatherapp.com",
            "priority": "high"
        }
    },
    {
        "messageId": "test-winter-warning-001",
        "subject": "❄️ Winter Storm Warning - Denver",
        "body": "",
        "recipient": "cnabilou@gmail.com",
        "timestamp": int(time.time() * 1000),
        "alertType": "PRECIPITATION_HEAVY",
        "severity": "WARNING",
        "location": {
            "zipCode": "80202",
            "city": "Denver",
            "state": "Colorado",
            "latitude": 39.7392,
            "longitude": -104.9903
        },
        "weatherConditions": {
            "currentTemperature": -8.0,
            "weatherCode": 75,
            "weatherDescription": "Heavy snow fall",
            "windSpeed": 45.0,
            "precipitation": 15.8
        },
        "metadata": {
            "from": "winter-alerts@weatherapp.com",
            "priority": "medium"
        }
    },
    {
        "messageId": "test-wind-warning-001",
        "subject": "💨 High Wind Warning - Chicago",
        "body": "",
        "recipient": "cnabilou@gmail.com",
        "timestamp": int(time.time() * 1000),
        "alertType": "WIND_WARNING",
        "severity": "WARNING",
        "location": {
            "zipCode": "60601",
            "city": "Chicago",
            "state": "Illinois",
            "latitude": 41.8781,
            "longitude": -87.6298
        },
        "weatherConditions": {
            "currentTemperature": 2.0,
            "weatherCode": 3,
            "weatherDescription": "Overcast with gusty winds",
            "windSpeed": 88.5,
            "precipitation": 0.0
        },
        "metadata": {
            "from": "wind-alerts@weatherapp.com",
            "priority": "medium"
        }
    },
    {
        "messageId": "test-info-forecast-001",
        "subject": "☁️ Daily Weather Forecast - Seattle",
        "body": "",
        "recipient": "cnabilou@gmail.com",
        "timestamp": int(time.time() * 1000),
        "alertType": "GENERAL_ALERT",
        "severity": "INFO",
        "location": {
            "zipCode": "98101",
            "city": "Seattle",
            "state": "Washington",
            "latitude": 47.6062,
            "longitude": -122.3321
        },
        "weatherConditions": {
            "currentTemperature": 18.5,
            "weatherCode": 2,
            "weatherDescription": "Partly cloudy with light rain",
            "windSpeed": 15.0,
            "precipitation": 2.5
        },
        "metadata": {
            "from": "daily-forecast@weatherapp.com",
            "priority": "low"
        }
    }
]


def load_env_file(env_path: str = ".env") -> Dict[str, str]:
    """Load environment variables from .env file"""
    env_vars = {}
    if os.path.exists(env_path):
        with open(env_path, 'r') as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#') and '=' in line:
                    key, value = line.split('=', 1)
                    env_vars[key] = value
    return env_vars


def delivery_report(err, msg):
    """Callback for message delivery reports"""
    if err is not None:
        print(f'❌ Message delivery failed: {err}')
    else:
        print(f'✅ Message delivered to {msg.topic()} [{msg.partition()}] at offset {msg.offset()}')


def create_producer(env_vars: Dict[str, str]) -> SerializingProducer:
    """Create Confluent Cloud Avro producer"""

    # Load Avro schema
    schema_path = os.path.join(os.path.dirname(__file__), '..', 'schemas', 'avro', 'weather-alert.avsc')
    with open(schema_path, 'r') as f:
        schema_str = f.read()

    # Schema Registry configuration
    schema_registry_conf = {
        'url': env_vars.get('KAFKA_SCHEMA_REGISTRY_URL'),
        'basic.auth.user.info': f"{env_vars.get('KAFKA_SCHEMA_REGISTRY_KEY')}:{env_vars.get('KAFKA_SCHEMA_REGISTRY_SECRET')}"
    }

    schema_registry_client = SchemaRegistryClient(schema_registry_conf)

    avro_serializer = AvroSerializer(
        schema_registry_client,
        schema_str,
        lambda msg, ctx: msg  # Direct dict serialization
    )

    # Kafka producer configuration
    producer_conf = {
        'bootstrap.servers': env_vars.get('KAFKA_BOOTSTRAP_SERVERS'),
        'security.protocol': 'SASL_SSL',
        'sasl.mechanism': 'PLAIN',
        'sasl.username': env_vars.get('KAFKA_SASL_USERNAME'),
        'sasl.password': env_vars.get('KAFKA_SASL_PASSWORD'),
        'key.serializer': StringSerializer('utf_8'),
        'value.serializer': avro_serializer
    }

    return SerializingProducer(producer_conf)


def send_message(producer: SerializingProducer, message: Dict[str, Any], topic: str = 'weather-alerts'):
    """Send a single message to the topic"""

    # Generate unique messageId with timestamp
    message['messageId'] = f"{message['messageId'].split('-')[0]}-{int(time.time())}"
    message['timestamp'] = int(time.time() * 1000)

    print(f"\n📤 Sending message:")
    print(f"   MessageId: {message['messageId']}")
    print(f"   Subject: {message['subject']}")
    print(f"   Severity: {message['severity']}")
    print(f"   Location: {message['location']['city']}, {message['location']['state']}")

    try:
        producer.produce(
            topic=topic,
            key=message['messageId'],
            value=message,
            on_delivery=delivery_report
        )
        producer.poll(0)
    except Exception as e:
        print(f"❌ Failed to send message: {e}")


def interactive_mode(producer: SerializingProducer):
    """Interactive mode to send custom messages"""
    print("\n🎯 Interactive Weather Alert Producer")
    print("=" * 50)

    while True:
        print("\nSelect severity level:")
        print("  1. CRITICAL (Red)")
        print("  2. SEVERE (Orange)")
        print("  3. WARNING (Amber)")
        print("  4. INFO (Blue)")
        print("  5. Send all samples")
        print("  0. Exit")

        choice = input("\nChoice: ").strip()

        if choice == '0':
            print("👋 Goodbye!")
            break
        elif choice == '5':
            for msg in SAMPLE_MESSAGES:
                send_message(producer, msg.copy())
                producer.flush()
                time.sleep(0.5)
            print(f"\n✅ Sent {len(SAMPLE_MESSAGES)} messages!")
        elif choice in ['1', '2', '3', '4']:
            severity_map = {
                '1': 'CRITICAL',
                '2': 'SEVERE',
                '3': 'WARNING',
                '4': 'INFO'
            }
            severity = severity_map[choice]

            # Find message with matching severity
            matching = [m for m in SAMPLE_MESSAGES if m['severity'] == severity]
            if matching:
                send_message(producer, matching[0].copy())
                producer.flush()
            else:
                print(f"❌ No sample message found for severity: {severity}")
        else:
            print("❌ Invalid choice!")


def main():
    parser = argparse.ArgumentParser(description='Send weather alerts to Confluent Cloud')
    parser.add_argument('--env', default='.env', help='Path to .env file')
    parser.add_argument('--topic', default='weather-alerts', help='Kafka topic name')
    parser.add_argument('--count', type=int, help='Number of random messages to send')
    parser.add_argument('--severity', choices=['CRITICAL', 'SEVERE', 'WARNING', 'INFO'],
                       help='Send only messages with this severity')
    parser.add_argument('--interactive', '-i', action='store_true', help='Interactive mode')
    parser.add_argument('--all', action='store_true', help='Send all sample messages')

    args = parser.parse_args()

    # Load environment variables
    env_path = os.path.join(os.path.dirname(__file__), '..', args.env)
    env_vars = load_env_file(env_path)

    if not env_vars.get('KAFKA_BOOTSTRAP_SERVERS'):
        print("❌ ERROR: KAFKA_BOOTSTRAP_SERVERS not found in .env file")
        sys.exit(1)

    print("🚀 Weather Alert Producer for Confluent Cloud")
    print("=" * 50)
    print(f"📡 Bootstrap Servers: {env_vars.get('KAFKA_BOOTSTRAP_SERVERS')}")
    print(f"📋 Schema Registry: {env_vars.get('KAFKA_SCHEMA_REGISTRY_URL')}")
    print(f"📬 Topic: {args.topic}")
    print("=" * 50)

    # Create producer
    try:
        producer = create_producer(env_vars)
    except Exception as e:
        print(f"❌ Failed to create producer: {e}")
        sys.exit(1)

    try:
        if args.interactive:
            interactive_mode(producer)
        elif args.all:
            print(f"\n📤 Sending all {len(SAMPLE_MESSAGES)} sample messages...")
            for msg in SAMPLE_MESSAGES:
                send_message(producer, msg.copy(), args.topic)
                producer.flush()
                time.sleep(0.5)
            print(f"\n✅ Successfully sent {len(SAMPLE_MESSAGES)} messages!")
        elif args.severity:
            matching = [m for m in SAMPLE_MESSAGES if m['severity'] == args.severity]
            print(f"\n📤 Sending {len(matching)} message(s) with severity: {args.severity}")
            for msg in matching:
                send_message(producer, msg.copy(), args.topic)
                producer.flush()
            print(f"\n✅ Successfully sent {len(matching)} message(s)!")
        elif args.count:
            import random
            print(f"\n📤 Sending {args.count} random message(s)...")
            for i in range(args.count):
                msg = random.choice(SAMPLE_MESSAGES).copy()
                send_message(producer, msg, args.topic)
                producer.flush()
                time.sleep(0.5)
            print(f"\n✅ Successfully sent {args.count} message(s)!")
        else:
            # Default: send all messages
            print(f"\n📤 Sending all {len(SAMPLE_MESSAGES)} sample messages...")
            print("💡 Tip: Use --interactive for interactive mode, --count N for N random messages")
            print()
            for msg in SAMPLE_MESSAGES:
                send_message(producer, msg.copy(), args.topic)
                producer.flush()
                time.sleep(0.5)
            print(f"\n✅ Successfully sent {len(SAMPLE_MESSAGES)} messages!")

        # Final flush
        print("\n⏳ Flushing remaining messages...")
        producer.flush()
        print("✅ All messages sent successfully!")

    except KeyboardInterrupt:
        print("\n\n⚠️  Interrupted by user")
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        producer.flush()


if __name__ == '__main__':
    main()
