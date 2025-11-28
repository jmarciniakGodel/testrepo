# File Upload Validation and Security Enhancements

## Overview

This document describes the comprehensive file upload validation and error handling improvements implemented to address security and reliability concerns in the meeting attendance CSV upload process.

## Problem Statement

The original implementation had several critical gaps:

1. **Insufficient type validation**: Only checked MIME types, which can be easily spoofed
2. **Missing content validation**: No validation of CSV structure or semantic correctness
3. **Non-atomic operations**: Partial data could persist even when validation failed later
4. **Poor error messages**: Generic errors without actionable guidance

## Solution Components

### 1. Layered File Validation (FileValidator.cs)

The new validation process implements multiple layers of defense:

#### Layer 1: MIME Type Check
```csharp
FileValidator.IsValidCsvMimeType(contentType)
```
Validates the HTTP Content-Type header. Accepts: `text/csv`, `application/csv`, `text/plain`, `application/vnd.ms-excel`

#### Layer 2: Magic Number / File Signature Detection
```csharp
FileValidator.ValidateCsvFileAsync(stream, fileName)
```
Performs deep content inspection:
- **BOM Detection**: Identifies UTF-8, UTF-16 LE/BE byte order marks
- **JSON Detection**: Detects JSON objects/arrays (limited to first 4KB for performance)
- **XML Detection**: Identifies XML declarations and tags
- **HTML Detection**: Detects HTML documents
- **Binary Detection**: Checks for PDF, ZIP, Office file signatures
- **ASCII Ratio**: Ensures content is primarily text-based

Returns a `ValidationResult` with:
- `IsValid`: Boolean success indicator
- `ErrorCode`: Specific error code (e.g., "TYPE_MISMATCH", "EMPTY_FILE", "BINARY_CONTENT")
- `ErrorMessage`: Human-readable error description
- `DetectedType`: Actual detected MIME type
- `OriginalExtension`: Original file extension for forensics

### 2. Semantic CSV Validation (CsvParser.cs)

After file type validation, the CSV content is validated for correctness:

#### For Teams CSV Format:
- **Required Sections**: Must contain "1. Summary" and "2. Participants" sections
- **Required Fields**:
  - Meeting title
  - Start time (valid date format)
  - At least one attendee row
  - Valid email addresses (RFC 5322 compliant regex)
- **Schema Validation**: Headers must contain "Name", "Email", "In-Meeting Duration"

#### For Simple CSV Format:
- **Minimum Lines**: At least 3 lines (title, header, data)
- **Required Headers**: Must contain "Name" and "Email" (case-insensitive)
- **Email Validation**: All emails must match RFC 5322 pattern
- **At Least One Attendee**: Must have at least one valid attendee with email

Returns a `CsvParseResult` with:
- `Success`: Boolean indicator
- `Data`: Parsed `CsvMeetingData` if successful
- `ErrorCode`: Specific error code (e.g., "NO_ATTENDEES", "INVALID_EMAIL_FORMAT")
- `ErrorMessage`: Detailed error description

### 3. Atomic Transaction Processing (MeetingUploadService.cs)

The upload process now uses database transactions to ensure atomicity:

```
┌─────────────────────────────────────────────┐
│ Begin Transaction                            │
├─────────────────────────────────────────────┤
│ Phase 1: Validate ALL Files                 │
│   - MIME type check                          │
│   - File signature check                     │
│   - Parse and validate CSV structure         │
│   - Cache parsed results                     │
│   - STOP if ANY file fails                   │
├─────────────────────────────────────────────┤
│ Phase 2: Process Data (only if all valid)   │
│   - Create meetings                          │
│   - Create/find attendants                   │
│   - Create attendance records                │
│   - Generate summary                         │
├─────────────────────────────────────────────┤
│ Commit Transaction                           │
│   - ALL data persisted                       │
│ OR                                           │
│ Rollback Transaction                         │
│   - NO data persisted                        │
└─────────────────────────────────────────────┘
```

**Key Features:**
- **Fail-Fast**: Validates ALL files before processing ANY
- **Cached Parsing**: Files are parsed once during validation; results are reused
- **Graceful Degradation**: Works with InMemory databases for testing
- **Comprehensive Logging**: Every step is logged for forensic analysis

### 4. Enhanced Error Responses (MeetingUploadController.cs)

The API now returns structured error responses:

```json
{
  "error": "File validation failed for response.csv: File appears to be JSON...",
  "errorCode": "TYPE_MISMATCH",
  "hint": "The file extension does not match the actual file content..."
}
```

**Error Codes:**
- `NO_FILES`: No files uploaded
- `TYPE_MISMATCH`: File type doesn't match extension
- `EMPTY_FILE`: File is empty
- `NO_ATTENDEES`: No valid attendees found
- `INVALID_EMAIL`: Invalid email format
- `INVALID_FORMAT`: Missing required fields or headers
- `VALIDATION_ERROR`: General validation failure
- `SERVER_ERROR`: Unexpected server error

## Security Improvements

1. **Content Type Mismatch Detection**: Files like `malicious.exe.csv` are detected and rejected
2. **JSON/XML/HTML Injection Prevention**: Non-CSV content is rejected before parsing
3. **Email Validation**: Prevents invalid/malicious email addresses from being stored
4. **Binary File Rejection**: Office documents, PDFs, executables are blocked
5. **Forensic Logging**: All validation failures are logged with full context

## Performance Considerations

1. **JSON Parsing Limit**: Only first 4KB is parsed to avoid DoS with large files
2. **Single Pass Parsing**: Files are parsed once; results cached for processing
3. **Fail-Fast Validation**: Stops at first invalid file to minimize wasted work
4. **Stream Processing**: Files are processed as streams, not loaded entirely into memory

## Testing

**51 tests** covering all scenarios:

- **FileValidatorTests** (10 tests): MIME type and file signature detection
- **CsvParserTests** (15 tests): CSV structure and semantic validation  
- **FileUploadIntegrationTests** (5 tests): End-to-end upload scenarios
- **RequirementsVerificationTests** (6 tests): Specific issue requirements
- **ControllerTests** (15 tests): API endpoint behavior

### Key Test Cases:
- ✅ Accept valid Teams CSV with proper structure
- ✅ Reject JSON file with `.csv` extension
- ✅ Reject empty files
- ✅ Reject CSV without attendees
- ✅ Reject CSV with invalid email formats
- ✅ Reject CSV missing required fields
- ✅ Atomic rollback when any file in batch fails
- ✅ No orphaned data on validation failures

## Logging

All validation steps are logged with structured data:

```
INFO: Starting validation of 2 files
INFO: Validating file: meeting.csv, Extension: .csv, ContentType: text/csv
INFO: File meeting.csv passed content validation. Detected type: text/csv
INFO: File meeting.csv passed CSV validation. Title: Team Meeting, Attendees: 5
WARN: File validation failed for bad.csv: Code=TYPE_MISMATCH, Message=File appears to be JSON, DetectedType=application/json, OriginalExtension=.csv
ERROR: Transaction rolled back due to error during file processing
```

This enables:
- **Forensic Analysis**: Track what went wrong and when
- **Security Monitoring**: Detect malicious upload attempts
- **Debugging**: Understand validation failures
- **Compliance**: Audit trail for data processing

## Backward Compatibility

The changes maintain backward compatibility:
- Existing valid CSV files continue to work
- API signatures unchanged
- Database schema unchanged
- Only validation logic enhanced
