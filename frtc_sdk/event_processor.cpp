#include "event_processor.h"


void EventProcessor::start() {
    running_ = true;
    thread_ = std::thread([this]() {
        while (running_) {
            std::function<void()> task;
            {
                std::unique_lock<std::mutex> lock(mutex_);
                condition_.wait(lock, [this]() { return !tasks_.empty() || !running_; });
                if (!running_ && tasks_.empty()) return;
                task = std::move(tasks_.front());
                tasks_.pop();
            }
            task();
        }
    });
}

void EventProcessor::stop() {
    {
        std::lock_guard<std::mutex> lock(mutex_);
        running_ = false;
    }
    condition_.notify_all();
    if (thread_.joinable()) thread_.join();
}
