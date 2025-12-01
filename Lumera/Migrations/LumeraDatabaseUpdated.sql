-- Lumera Event Planning Platform Database Schema for MySQL
CREATE DATABASE Lumera;
USE Lumera;

-- 1. Core User Tables
CREATE TABLE Users (
    UserID INT PRIMARY KEY AUTO_INCREMENT,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role ENUM('Client', 'Organizer', 'Supplier', 'Admin') NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    AvatarURL VARCHAR(500),
    IsActive BOOLEAN DEFAULT TRUE,
    IsApproved BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastLogin DATETIME
);

CREATE TABLE Clients (
    ClientID INT PRIMARY KEY AUTO_INCREMENT,
    UserID INT,
    DateOfBirth DATE,
    PreferredEventTypes JSON,
    NewsletterSubscription BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Organizers (
    OrganizerID INT PRIMARY KEY AUTO_INCREMENT,
    UserID INT,
    BusinessName VARCHAR(255) NOT NULL,
    BusinessDescription TEXT,
    BusinessLicense VARCHAR(100),
    YearsOfExperience INT,
    ServiceAreas JSON,
    AverageRating DECIMAL(3,2) DEFAULT 0.00,
    TotalReviews INT DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Suppliers (
    SupplierID INT PRIMARY KEY AUTO_INCREMENT,
    UserID INT,
    BusinessName VARCHAR(255) NOT NULL,
    BusinessDescription TEXT,
    ServiceCategory VARCHAR(100) NOT NULL,
    ServiceAreas JSON,
    YearsOfExperience INT,
    AverageRating DECIMAL(3,2) DEFAULT 0.00,
    TotalReviews INT DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- 2. Event Management Tables
CREATE TABLE Events (
    EventID INT PRIMARY KEY AUTO_INCREMENT,
    ClientID INT,
    OrganizerID INT,
    EventName VARCHAR(255) NOT NULL,
    EventType VARCHAR(100) NOT NULL,
    EventDescription TEXT,
    EventDate DATETIME NOT NULL,
    Budget DECIMAL(10,2),
    GuestCount INT,
    Location VARCHAR(500),
    Status ENUM('Draft', 'Planning', 'Confirmed', 'In Progress', 'Completed', 'Cancelled') DEFAULT 'Draft',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (ClientID) REFERENCES Clients(ClientID) ON DELETE CASCADE,
    FOREIGN KEY (OrganizerID) REFERENCES Organizers(OrganizerID) ON DELETE SET NULL
);

CREATE TABLE EventTimeline (
    TimelineID INT PRIMARY KEY AUTO_INCREMENT,
    EventID INT,
    TaskName VARCHAR(255) NOT NULL,
    TaskDescription TEXT,
    DueDate DATETIME,
    Status ENUM('Pending', 'In Progress', 'Completed') DEFAULT 'Pending',
    AssignedToUserID INT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE,
    FOREIGN KEY (AssignedToUserID) REFERENCES Users(UserID) ON DELETE SET NULL
);

CREATE TABLE EventChecklist (
    ChecklistID INT PRIMARY KEY AUTO_INCREMENT,
    EventID INT,
    ItemName VARCHAR(255) NOT NULL,
    IsCompleted BOOLEAN DEFAULT FALSE,
    Category VARCHAR(100),
    DueDate DATETIME,
    AssignedToUserID INT,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE,
    FOREIGN KEY (AssignedToUserID) REFERENCES Users(UserID) ON DELETE SET NULL
);

-- 3. Services & Bookings Tables
CREATE TABLE Services (
    ServiceID INT PRIMARY KEY AUTO_INCREMENT,
    ProviderID INT NOT NULL,
    ProviderType ENUM('Organizer', 'Supplier') NOT NULL,
    ServiceName VARCHAR(255) NOT NULL,
    ServiceDescription TEXT,
    Category VARCHAR(100) NOT NULL,
    BasePrice DECIMAL(10,2),
    PriceType VARCHAR(50),
    Location VARCHAR(500),
    IsActive BOOLEAN DEFAULT TRUE,
    IsApproved BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    AverageRating DECIMAL(3,2) DEFAULT 0.00,
    TotalReviews INT DEFAULT 0
);

CREATE TABLE ServiceGallery (
    GalleryID INT PRIMARY KEY AUTO_INCREMENT,
    ServiceID INT,
    ImageURL VARCHAR(500) NOT NULL,
    Caption VARCHAR(255),
    DisplayOrder INT DEFAULT 0,
    FOREIGN KEY (ServiceID) REFERENCES Services(ServiceID) ON DELETE CASCADE
);

CREATE TABLE Bookings (
    BookingID INT PRIMARY KEY AUTO_INCREMENT,
    EventID INT,
    ServiceID INT,
    ClientID INT,
    ProviderID INT NOT NULL,
    ProviderType ENUM('Organizer', 'Supplier') NOT NULL,
    BookingDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    EventDate DATETIME NOT NULL,
    ServiceDetails TEXT,
    QuoteAmount DECIMAL(10,2),
    FinalAmount DECIMAL(10,2),
    Status ENUM('Pending', 'Negotiating', 'Accepted', 'Confirmed', 'Completed', 'Cancelled', 'Rejected') DEFAULT 'Pending',
    ClientNotes TEXT,
    ProviderNotes TEXT,
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE CASCADE,
    FOREIGN KEY (ServiceID) REFERENCES Services(ServiceID) ON DELETE CASCADE,
    FOREIGN KEY (ClientID) REFERENCES Clients(ClientID) ON DELETE CASCADE
);

CREATE TABLE BookingMessages (
    MessageID INT PRIMARY KEY AUTO_INCREMENT,
    BookingID INT,
    SenderID INT,
    MessageText TEXT NOT NULL,
    AttachmentURL VARCHAR(500),
    IsRead BOOLEAN DEFAULT FALSE,
    SentAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BookingID) REFERENCES Bookings(BookingID) ON DELETE CASCADE,
    FOREIGN KEY (SenderID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- 4. Reviews & Ratings Tables
CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY AUTO_INCREMENT,
    BookingID INT,
    ReviewerID INT,
    RevieweeID INT NOT NULL,
    RevieweeType ENUM('Organizer', 'Supplier') NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    ReviewText TEXT,
    IsApproved BOOLEAN DEFAULT FALSE,
    IsEdited BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (BookingID) REFERENCES Bookings(BookingID) ON DELETE CASCADE,
    FOREIGN KEY (ReviewerID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE ReviewLikes (
    LikeID INT PRIMARY KEY AUTO_INCREMENT,
    ReviewID INT,
    UserID INT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ReviewID) REFERENCES Reviews(ReviewID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- 5. Messaging System Tables
CREATE TABLE Conversations (
    ConversationID INT PRIMARY KEY AUTO_INCREMENT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastMessageAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    EventID INT,
    ConversationType ENUM('Direct', 'Group') DEFAULT 'Direct',
    FOREIGN KEY (EventID) REFERENCES Events(EventID) ON DELETE SET NULL
);

CREATE TABLE ConversationParticipants (
    ParticipantID INT PRIMARY KEY AUTO_INCREMENT,
    ConversationID INT,
    UserID INT,
    JoinedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LeftAt DATETIME,
    FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID) ON DELETE CASCADE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Messages (
    MessageID INT PRIMARY KEY AUTO_INCREMENT,
    ConversationID INT,
    SenderID INT,
    MessageText TEXT NOT NULL,
    AttachmentURL VARCHAR(500),
    MessageType ENUM('Text', 'Image', 'File', 'System') DEFAULT 'Text',
    IsRead BOOLEAN DEFAULT FALSE,
    SentAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID) ON DELETE CASCADE,
    FOREIGN KEY (SenderID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- 6. Financial Tables
CREATE TABLE Transactions (
    TransactionID INT PRIMARY KEY AUTO_INCREMENT,
    BookingID INT,
    PayerID INT,
    PayeeID INT,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionType ENUM('Booking', 'Deposit', 'Final Payment', 'Refund'),
    Status ENUM('Pending', 'Completed', 'Failed', 'Refunded') DEFAULT 'Pending',
    PaymentMethod VARCHAR(100),
    TransactionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    StripePaymentIntentID VARCHAR(255),
    FOREIGN KEY (BookingID) REFERENCES Bookings(BookingID) ON DELETE CASCADE,
    FOREIGN KEY (PayerID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (PayeeID) REFERENCES Users(UserID) ON DELETE CASCADE
);

CREATE TABLE Payouts (
    PayoutID INT PRIMARY KEY AUTO_INCREMENT,
    PayeeID INT,
    Amount DECIMAL(10,2) NOT NULL,
    Status ENUM('Pending', 'Processing', 'Completed', 'Failed') DEFAULT 'Pending',
    PayoutMethod VARCHAR(100),
    ProcessedAt DATETIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PayeeID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- 7. Platform Management Tables
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY AUTO_INCREMENT,
    CategoryName VARCHAR(100) NOT NULL,
    CategoryDescription TEXT,
    ParentCategoryID INT,
    IsActive BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID) ON DELETE SET NULL
);

CREATE TABLE PlatformAnalytics (
    AnalyticID INT PRIMARY KEY AUTO_INCREMENT,
    MetricDate DATE NOT NULL,
    TotalUsers INT DEFAULT 0,
    NewSignups INT DEFAULT 0,
    TotalBookings INT DEFAULT 0,
    TotalRevenue DECIMAL(15,2) DEFAULT 0.00,
    ActiveEvents INT DEFAULT 0,
    PopularCategories JSON
);

CREATE TABLE AdminActions (
    ActionID INT PRIMARY KEY AUTO_INCREMENT,
    AdminID INT,
    ActionType VARCHAR(100) NOT NULL,
    TargetType VARCHAR(50),
    TargetID INT,
    Description TEXT,
    ActionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AdminID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_role ON Users(Role);
CREATE INDEX idx_events_client_id ON Events(ClientID);
CREATE INDEX idx_events_organizer_id ON Events(OrganizerID);
CREATE INDEX idx_events_status ON Events(Status);
CREATE INDEX idx_bookings_event_id ON Bookings(EventID);
CREATE INDEX idx_bookings_service_id ON Bookings(ServiceID);
CREATE INDEX idx_bookings_status ON Bookings(Status);
CREATE INDEX idx_services_provider ON Services(ProviderID, ProviderType);
CREATE INDEX idx_services_category ON Services(Category);
CREATE INDEX idx_reviews_reviewee ON Reviews(RevieweeID, RevieweeType);
CREATE INDEX idx_messages_conversation_id ON Messages(ConversationID);
CREATE INDEX idx_transactions_booking_id ON Transactions(BookingID);
CREATE INDEX idx_conversation_participants_user_id ON ConversationParticipants(UserID);
CREATE INDEX idx_service_gallery_service_id ON ServiceGallery(ServiceID);
CREATE INDEX idx_events_date ON Events(EventDate);
CREATE INDEX idx_bookings_event_date ON Bookings(EventDate);

USE Lumera;

-- Add IsActive column to Organizers table
ALTER TABLE Organizers ADD COLUMN IsActive BOOLEAN DEFAULT TRUE;

-- Add IsActive column to Suppliers table  
ALTER TABLE Suppliers ADD COLUMN IsActive BOOLEAN DEFAULT TRUE;

-- Update existing records to be active
UPDATE Organizers SET IsActive = TRUE;
UPDATE Suppliers SET IsActive = TRUE;
