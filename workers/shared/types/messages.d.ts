/**
 * Message types for RabbitMQ communication
 * These interfaces define the structure of messages exchanged between services
 */
/**
 * Raw analysis message published by WebAPI to raw-analysis-queue
 */
export interface RawAnalysisMessage {
    analysisId: number;
    userId: number;
    userEmail: string;
    imageUrls: string[];
    cropType?: string;
    location?: string;
    language?: string;
    timestamp: string;
}
/**
 * Provider-routed analysis message sent by Dispatcher to provider-specific queues
 * (openai-analysis-queue, gemini-analysis-queue, anthropic-analysis-queue)
 */
export interface ProviderAnalysisMessage extends RawAnalysisMessage {
    provider: 'openai' | 'gemini' | 'anthropic';
    routingTimestamp: string;
    attemptNumber: number;
}
/**
 * Analysis result message published to analysis-results-queue
 * Consumed by PlantAnalysisWorker for database persistence
 */
export interface AnalysisResultMessage {
    analysisId: number;
    userId: number;
    userEmail: string;
    provider: 'openai' | 'gemini' | 'anthropic';
    diseaseNames: string[];
    diseaseDescriptions: string[];
    treatment: string;
    preventiveMeasures: string;
    additionalNotes?: string;
    processingTimeMs: number;
    tokenUsage?: number;
    costUsd?: number;
    modelUsed: string;
    timestamp: string;
    success: boolean;
    error?: string;
}
/**
 * Dead letter queue message for failed processing
 */
export interface DeadLetterMessage {
    originalMessage: RawAnalysisMessage | ProviderAnalysisMessage;
    error: string;
    failureTimestamp: string;
    attemptCount: number;
    lastProvider?: string;
}
/**
 * Health check message for monitoring worker status
 */
export interface WorkerHealthMessage {
    workerId: string;
    provider: 'openai' | 'gemini' | 'anthropic';
    status: 'healthy' | 'degraded' | 'unhealthy';
    queueDepth: number;
    processingRate: number;
    lastProcessedTimestamp: string;
    errorRate: number;
}
//# sourceMappingURL=messages.d.ts.map