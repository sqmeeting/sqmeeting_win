#pragma once

#include <string>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <functional>
#include <tuple>


class EventProcessor {
public:
    explicit EventProcessor(const std::string &name) : name_(name), running_(false) {}

    void start();

    void stop();

    template<typename F, typename... Args>
    void post(F&& f, Args&&... args) {
        auto task = std::bind(std::forward<F>(f), std::forward<Args>(args)...);
        {
            std::lock_guard<std::mutex> lock(mutex_);
            tasks_.push(task);
        }
        condition_.notify_all();
    };

private:
    std::string name_;
    std::thread thread_;
    std::mutex mutex_;
    std::condition_variable condition_;
    std::queue<std::function<void()>> tasks_;
    bool running_;
};
